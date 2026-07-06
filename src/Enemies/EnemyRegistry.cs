using System.Collections.ObjectModel;
using KoraxLib.Internal.Registration;
using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Enemies;

/// <summary>
/// 敌人和遭遇内容的声明式注册入口。
/// </summary>
/// <remarks>
/// 这个 registry 只负责“收集消费端想注册什么”，不会立刻修改 STS2 的 `ModelDb`
/// 或 act encounter 列表。真正把这些类型合并进游戏数据的 Harmony patch 会在后续步骤实现。
///
/// 这种拆分可以把消费端 API 和 STS2 初始化时序隔离开：消费端只声明自己的
/// `MonsterModel` / `EncounterModel` 类型，KoraxLib 在安全的初始化阶段统一应用。
/// </remarks>
public static class EnemyRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly HashSet<Type> MonsterTypes = [];
    private static readonly HashSet<Type> GlobalEncounterTypes = [];
    private static readonly Dictionary<Type, HashSet<Type>> ActEncounterTypes = [];

    /// <summary>
    /// 当前内容注册生命周期状态。
    /// </summary>
    /// <remarks>
    /// 该状态来自 KoraxLib 全局注册闸门。消费端可以读取它来诊断“为什么注册失败”，
    /// 但不能通过 `EnemyRegistry` 控制生命周期切换。
    /// </remarks>
    public static KoraxRegistrationState State => RegistrationLifecycle.State;

    /// <summary>
    /// 已声明的 monster 类型快照。
    /// </summary>
    /// <remarks>
    /// 返回值是防御性副本。调用方修改返回集合不会影响 KoraxLib 内部注册表。
    /// </remarks>
    public static IReadOnlyCollection<Type> RegisteredMonsters
    {
        get
        {
            lock (SyncRoot)
            {
                return new ReadOnlyCollection<Type>(MonsterTypes.ToArray());
            }
        }
    }

    /// <summary>
    /// 已声明的全局 encounter 类型快照。
    /// </summary>
    /// <remarks>
    /// 全局 encounter 表示后续 patch 会尝试把该 encounter 合并进所有支持的 act。
    /// 当前步骤只记录声明，不做实际合并。
    /// </remarks>
    public static IReadOnlyCollection<Type> RegisteredGlobalEncounters
    {
        get
        {
            lock (SyncRoot)
            {
                return new ReadOnlyCollection<Type>(GlobalEncounterTypes.ToArray());
            }
        }
    }

    /// <summary>
    /// 已声明的 act-scoped encounter 类型快照。
    /// </summary>
    /// <remarks>
    /// 字典 key 是 `ActModel` 子类型，value 是要加入该 act 的 `EncounterModel` 子类型集合。
    /// 字典和每个 value 都是防御性副本，避免消费端绕过注册生命周期直接修改内部状态。
    /// </remarks>
    public static IReadOnlyDictionary<Type, IReadOnlyCollection<Type>> RegisteredActEncounters
    {
        get
        {
            lock (SyncRoot)
            {
                var snapshot = ActEncounterTypes.ToDictionary(
                    pair => pair.Key,
                    static pair => (IReadOnlyCollection<Type>)new ReadOnlyCollection<Type>(pair.Value.ToArray()));

                return new ReadOnlyDictionary<Type, IReadOnlyCollection<Type>>(snapshot);
            }
        }
    }

    /// <summary>
    /// 声明一个自定义 monster 类型。
    /// </summary>
    /// <typeparam name="TMonster">要注册的 `MonsterModel` 子类型。</typeparam>
    public static void RegisterMonster<TMonster>()
        where TMonster : MonsterModel
    {
        RegisterMonster(typeof(TMonster));
    }

    /// <summary>
    /// 声明一个自定义 monster 类型。
    /// </summary>
    /// <param name="monsterType">要注册的 `MonsterModel` 子类型。</param>
    /// <exception cref="ArgumentNullException"><paramref name="monsterType" /> 为 null。</exception>
    /// <exception cref="ArgumentException"><paramref name="monsterType" /> 不是 `MonsterModel` 子类型。</exception>
    /// <exception cref="InvalidOperationException">注册窗口已经冻结。</exception>
    public static void RegisterMonster(Type monsterType)
    {
        RegistrationLifecycle.ThrowIfFrozen(nameof(EnemyRegistry));
        EnsureAssignableTo<MonsterModel>(monsterType, nameof(monsterType));

        lock (SyncRoot)
        {
            if (!MonsterTypes.Add(monsterType))
            {
                Entry.Logger.Debug($"EnemyRegistry ignored duplicate monster registration: {monsterType.FullName}.");
            }
        }
    }

    /// <summary>
    /// 声明一个只加入指定 act 的 encounter 类型。
    /// </summary>
    /// <typeparam name="TAct">目标 `ActModel` 子类型。</typeparam>
    /// <typeparam name="TEncounter">要加入该 act 的 `EncounterModel` 子类型。</typeparam>
    public static void RegisterActEncounter<TAct, TEncounter>()
        where TAct : ActModel
        where TEncounter : EncounterModel
    {
        RegisterActEncounter(typeof(TAct), typeof(TEncounter));
    }

    /// <summary>
    /// 声明一个只加入指定 act 的 encounter 类型。
    /// </summary>
    /// <param name="actType">目标 `ActModel` 子类型。</param>
    /// <param name="encounterType">要加入该 act 的 `EncounterModel` 子类型。</param>
    /// <exception cref="ArgumentNullException"><paramref name="actType" /> 或 <paramref name="encounterType" /> 为 null。</exception>
    /// <exception cref="ArgumentException">参数类型不满足 STS2 模型继承关系。</exception>
    /// <exception cref="InvalidOperationException">注册窗口已经冻结。</exception>
    public static void RegisterActEncounter(Type actType, Type encounterType)
    {
        RegistrationLifecycle.ThrowIfFrozen(nameof(EnemyRegistry));
        EnsureAssignableTo<ActModel>(actType, nameof(actType));
        EnsureAssignableTo<EncounterModel>(encounterType, nameof(encounterType));

        lock (SyncRoot)
        {
            if (!ActEncounterTypes.TryGetValue(actType, out var encounters))
            {
                encounters = [];
                ActEncounterTypes.Add(actType, encounters);
            }

            if (!encounters.Add(encounterType))
            {
                Entry.Logger.Debug(
                    $"EnemyRegistry ignored duplicate act encounter registration: {encounterType.FullName} for act {actType.FullName}.");
            }
        }
    }

    /// <summary>
    /// 声明一个全局 encounter 类型。
    /// </summary>
    /// <typeparam name="TEncounter">要加入所有支持 act 的 `EncounterModel` 子类型。</typeparam>
    public static void RegisterGlobalEncounter<TEncounter>()
        where TEncounter : EncounterModel
    {
        RegisterGlobalEncounter(typeof(TEncounter));
    }

    /// <summary>
    /// 声明一个全局 encounter 类型。
    /// </summary>
    /// <param name="encounterType">要加入所有支持 act 的 `EncounterModel` 子类型。</param>
    /// <exception cref="ArgumentNullException"><paramref name="encounterType" /> 为 null。</exception>
    /// <exception cref="ArgumentException"><paramref name="encounterType" /> 不是 `EncounterModel` 子类型。</exception>
    /// <exception cref="InvalidOperationException">注册窗口已经冻结。</exception>
    public static void RegisterGlobalEncounter(Type encounterType)
    {
        RegistrationLifecycle.ThrowIfFrozen(nameof(EnemyRegistry));
        EnsureAssignableTo<EncounterModel>(encounterType, nameof(encounterType));

        lock (SyncRoot)
        {
            if (!GlobalEncounterTypes.Add(encounterType))
            {
                Entry.Logger.Debug($"EnemyRegistry ignored duplicate global encounter registration: {encounterType.FullName}.");
            }
        }
    }

    private static void EnsureAssignableTo<TBase>(Type type, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(type, parameterName);

        if (typeof(TBase).IsAssignableFrom(type))
        {
            return;
        }

        throw new ArgumentException(
            $"Type '{type.FullName}' must inherit from '{typeof(TBase).FullName}'.",
            parameterName);
    }
}
