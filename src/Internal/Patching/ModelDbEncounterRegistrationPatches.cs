using System.Reflection;
using HarmonyLib;
using KoraxLib.Enemies;
using KoraxLib.Internal.Registration;
using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Internal.Patching;

/// <summary>
/// 将 <see cref="EnemyRegistry" /> 中声明的 encounter 类型动态合并进各 act 的
/// <see cref="ActModel.GenerateAllEncounters" /> 结果，并确保原版扫描漏掉的 encounter model
/// 在 <see cref="ModelDb.InitIds" /> 之前注入 <see cref="ModelDb" />。
/// </summary>
/// <remarks>
/// 这个 patch 文件与 <see cref="ModelDbEnemyRegistrationPatches" /> 协同工作：
/// 那边负责 monster model 的冻结和注入，这边负责 encounter model 的注入和
/// act encounter list 的动态合并。
///
/// <c>ActModel.AllEncounters</c> 缓存 <c>GenerateAllEncounters()</c> 的结果，
/// <c>ModelDb.Preload</c> 会读取该属性。因此 encounter 合并必须在 Preload 之前完成。
/// 实现方式是在 <c>ModelDb.Init</c> 阶段动态发现每个具体 act 类型的
/// <c>GenerateAllEncounters</c> 实现并安装 postfix。
/// </remarks>
[HarmonyPatch]
internal static class ModelDbEncounterRegistrationPatches
{
    private static readonly object SyncRoot = new();
    private static readonly HashSet<MethodInfo> PatchedEncounterGenerators = [];

    // ═══════════════════════════════════════════════════════════════
    //  ModelDb.Init 前缀：动态安装 act encounter postfix
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 STS2 开始初始化模型数据库时，扫描已加载的所有具体 <see cref="ActModel" /> 子类型，
    /// 为每个子类型的具体 <see cref="ActModel.GenerateAllEncounters" /> 实现动态安装 Harmony postfix。
    /// </summary>
    /// <remarks>
    /// 必须在 <c>ModelDb.Init</c> 前缀执行（而不是更晚的阶段），因为：
    /// <list type="number">
    /// <item>此时所有 mod assembly 已加载，可以完整扫描 ActModel 子类型。</item>
    /// <item>patch 安装后，后续 <c>ModelDb.Preload</c> 调用
    /// <c>ActModel.AllEncounters</c> 时会自动触发合并逻辑。</item>
    /// </list>
    /// 使用 <see cref="PatchedEncounterGenerators" /> 确保同一个方法实现不会被重复 patch。
    /// </remarks>
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    [HarmonyPrefix]
    public static void PrepareRegisteredEncountersBeforeModelDbInit()
    {
        RegistrationLifecycle.Freeze();
        InjectDynamicRegisteredEncountersIntoModelDb();

        var actTypes = FindConcreteActModelTypes();
        foreach (var actType in actTypes)
        {
            var original = AccessTools.Method(actType, nameof(ActModel.GenerateAllEncounters));
            if (original is null)
            {
                Entry.Logger.Debug(
                    $"ModelDbEncounterRegistrationPatches skipped {actType.FullName}: GenerateAllEncounters not found.");
                continue;
            }

            PatchEncounterGenerator(original);
        }
    }

    private static void PatchEncounterGenerator(MethodInfo original)
    {
        lock (SyncRoot)
        {
            if (!PatchedEncounterGenerators.Add(original))
            {
                return;
            }
        }

        var postfix = new HarmonyMethod(
            typeof(ModelDbEncounterRegistrationPatches),
            nameof(MergeRegisteredEncountersIntoAct));
        Entry.Harmony.Patch(original, postfix: postfix);
    }

    // ═══════════════════════════════════════════════════════════════
    //  动态安装的 ActModel.GenerateAllEncounters postfix
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 把 <see cref="EnemyRegistry" /> 中声明的 encounter 合并进当前 act 的 encounter 列表。
    /// </summary>
    /// <param name="__instance">当前正在生成 encounter 的 act 实例。</param>
    /// <param name="__result">原版 <c>GenerateAllEncounters()</c> 返回的 encounter 序列。</param>
    /// <remarks>
    /// 合并顺序和去重规则：
    /// <list type="number">
    /// <item>保留原版 encounter 的原始顺序。</item>
    /// <item>追加该 act 专属的 registered act encounters。</item>
    /// <item>追加 registered global encounters。</item>
    /// <item>已存在的 encounter（按 <c>EncounterModel.Id</c> 判断）不会重复添加。</item>
    /// </list>
    /// 因为静态 DLL 类型会被原版 <c>ModelDb.Init</c> 扫描，动态类型会由 KoraxLib prefix 注入，
    /// 所以 <c>ModelDb.GetById</c> / <c>ModelDb.GetId</c> 此时可以正常解析。
    /// </remarks>
    public static void MergeRegisteredEncountersIntoAct(ActModel __instance, ref IEnumerable<EncounterModel> __result)
    {
        var encounters = __result as EncounterModel[] ?? __result.ToArray();
        var knownIds = encounters.Select(static encounter => encounter.Id).ToHashSet();
        var merged = new List<EncounterModel>(encounters);

        var actType = __instance.GetType();

        // 首先追加该 act 专属的 encounter
        var registeredActEncounters = EnemyRegistry.RegisteredActEncounters;
        if (registeredActEncounters.TryGetValue(actType, out var actEncounterTypes))
        {
            foreach (var encType in actEncounterTypes)
            {
                AppendEncounterIfNew(encType, knownIds, merged);
            }
        }

        // 然后追加全局 encounter
        foreach (var encType in EnemyRegistry.RegisteredGlobalEncounters)
        {
            AppendEncounterIfNew(encType, knownIds, merged);
        }

        __result = merged;
    }

    private static void AppendEncounterIfNew(Type encType, HashSet<ModelId> knownIds, List<EncounterModel> encounters)
    {
        var encounter = ModelDb.GetById<EncounterModel>(ModelDb.GetId(encType));
        if (knownIds.Add(encounter.Id))
        {
            encounters.Add(encounter);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ModelDb.Init 注入：动态类型前置，静态类型后置兜底
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 在原版模型扫描开始前，把动态程序集中的 registered encounter 类型注入
    /// <see cref="ModelDb" />。
    /// </summary>
    /// <remarks>
    /// 静态 DLL 中的 encounter 类型会被 STS2 原版 <c>ModelDb.Init</c> 自动扫描，不能在 prefix
    /// 里提前构造，否则原版扫描再次构造时会触发 duplicate canonical model。动态程序集类型通常
    /// 不会被原版扫描拾取，因此这里仅提前注入动态类型。
    ///
    /// 取 <see cref="EnemyRegistry" /> 快照时，注册窗口已经冻结，快照内容不会再变化。
    /// </remarks>
    private static void InjectDynamicRegisteredEncountersIntoModelDb()
    {
        foreach (var encType in RegisteredEncounterTypes())
        {
            if (encType.Assembly.IsDynamic)
            {
                ModelDb.Inject(encType);
            }
        }
    }

    /// <summary>
    /// 在原版模型扫描结束后，兜底注入所有仍未被扫描拾取的 registered encounter 类型。
    /// </summary>
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    [HarmonyPostfix]
    public static void InjectRegisteredEncountersAfterModelDbInit()
    {
        foreach (var encType in RegisteredEncounterTypes())
        {
            ModelDb.Inject(encType);
        }
    }

    private static IEnumerable<Type> RegisteredEncounterTypes()
    {
        return EnemyRegistry.RegisteredActEncounters.Values
            .SelectMany(static encounterTypes => encounterTypes)
            .Concat(EnemyRegistry.RegisteredGlobalEncounters)
            .Distinct();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ActModel 子类型发现
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 扫描当前 AppDomain 中所有已加载程序集，找出所有具体的非抽象 <see cref="ActModel" /> 子类型。
    /// </summary>
    /// <remarks>
    /// STS2 原版 act（如 <c>Act1Model</c>、<c>Act2Model</c>）和 mod 添加的自定义 act 都在扫描范围内。
    /// 跳过无法完整枚举类型的程序集（可能是原生互操作程序集、缺失依赖或已卸载的程序集）。
    /// </remarks>
    private static List<Type> FindConcreteActModelTypes()
    {
        var result = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (Exception ex) when (ex is ReflectionTypeLoadException or TypeLoadException or FileLoadException or BadImageFormatException)
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                {
                    continue;
                }

                if (typeof(ActModel).IsAssignableFrom(type))
                {
                    result.Add(type);
                }
            }
        }

        return result;
    }
}
