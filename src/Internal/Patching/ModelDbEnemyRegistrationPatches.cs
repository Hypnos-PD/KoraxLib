using HarmonyLib;
using KoraxLib.Enemies;
using KoraxLib.Internal.Registration;
using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Internal.Patching;

/// <summary>
/// 将 <see cref="EnemyRegistry" /> 中声明的 monster 类型接入 STS2 的 <see cref="ModelDb" />。
/// </summary>
/// <remarks>
/// 这里故意只处理 monster model 本身，不处理 encounter pool。原因是两者处在不同的初始化路径：
/// monster model 需要进入 <c>ModelDb</c> 的 canonical model 字典，而 encounter 是否出现在某个 act
/// 里要等后续 patch <c>ActModel.GenerateAllEncounters()</c>。
/// </remarks>
[HarmonyPatch]
internal static class ModelDbEnemyRegistrationPatches
{
    /// <summary>
    /// 在 STS2 开始建立模型数据库前关闭内容注册窗口。
    /// </summary>
    /// <remarks>
    /// <c>ModelDb.Init()</c> 之后，模型 ID、序列化缓存和预加载流程会陆续消费模型集合。
    /// 如果此时仍允许消费端注册新内容，后续系统可能看到不同版本的模型列表。因此 freeze 必须发生在
    /// <c>ModelDb.Init()</c> 的入口处，而不是等到 <c>ModelDb.Monsters</c> 被读取时才发生。
    /// </remarks>
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    [HarmonyPrefix]
    public static void FreezeRegistrationsBeforeModelDbInit()
    {
        RegistrationLifecycle.Freeze();
    }

    /// <summary>
    /// 在原版模型扫描结束后，把注册过但未被原版扫描拾取的 monster 类型注入 <see cref="ModelDb" />。
    /// </summary>
    /// <remarks>
    /// 静态 DLL 里的 mod 类型通常会被 STS2 的 subtype scan 自动发现；动态程序集或扫描遗漏的类型则不会。
    /// 使用 <see cref="ModelDb.Inject" /> 放在 <c>ModelDb.Init()</c> postfix，可以避免对已发现的静态类型重复构造，
    /// 同时仍保证后续 <c>ModelDb.InitIds()</c> 和序列化缓存初始化能看到 KoraxLib 注册的 monster。
    /// </remarks>
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    [HarmonyPostfix]
    public static void InjectRegisteredMonstersAfterModelDbInit()
    {
        foreach (var monsterType in EnemyRegistry.RegisteredMonsters)
        {
            ModelDb.Inject(monsterType);
        }
    }

    /// <summary>
    /// 让 <see cref="ModelDb.Monsters" /> 枚举包含 KoraxLib 注册的 monster。
    /// </summary>
    /// <param name="__result">原版 getter 返回的 monster 序列。</param>
    /// <remarks>
    /// <c>ModelDb.Monsters</c> 本身不是缓存字段，而是从 act encounters 推导出来的序列。
    /// 独立注册的 monster 可能暂时还没有 encounter 引用它们，因此需要在 getter 层追加，
    /// 让调试命令、统计、其他模组扫描等“枚举所有 monster”的路径能看到它们。
    /// </remarks>
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Monsters), MethodType.Getter)]
    [HarmonyPostfix]
    public static void AppendRegisteredMonsters(ref IEnumerable<MonsterModel> __result)
    {
        var monsters = __result as MonsterModel[] ?? __result.ToArray();
        var knownIds = monsters.Select(static monster => monster.Id).ToHashSet();
        var appended = new List<MonsterModel>();

        foreach (var monsterType in EnemyRegistry.RegisteredMonsters)
        {
            var monster = ModelDb.GetById<MonsterModel>(ModelDb.GetId(monsterType));
            if (knownIds.Add(monster.Id))
            {
                appended.Add(monster);
            }
        }

        __result = monsters.Concat(appended);
    }
}
