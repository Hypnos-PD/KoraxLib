using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace KoraxLib.Enemies;

/// <summary>
/// KoraxLib 对一个 STS2 敌方怪物 creature 的安全上下文包装。
/// </summary>
public sealed record EnemyContext
{
    /// <summary>
    /// 创建一个经过校验的敌人上下文。
    /// </summary>
    public EnemyContext(ICombatState combatState, Creature creature, MonsterModel monster)
    {
        CombatState = EnsureCombatState(combatState, creature);
        Creature = creature;
        Monster = EnsureEnemyMonster(creature, monster);
    }

    /// <summary>
    /// 敌人所属的战斗状态。
    /// </summary>
    public ICombatState CombatState { get; }

    /// <summary>
    /// STS2 运行时 creature。
    /// </summary>
    public Creature Creature { get; }

    /// <summary>
    /// 与 creature 绑定的 monster model。
    /// </summary>
    public MonsterModel Monster { get; }

    /// <summary>
    /// 从 STS2 hook 参数创建敌人上下文；非敌方 monster 会被过滤掉。
    /// </summary>
    internal static EnemyContext? TryCreate(ICombatState combatState, Creature creature)
    {
        ArgumentNullException.ThrowIfNull(combatState);
        ArgumentNullException.ThrowIfNull(creature);

        if (!TryGetEnemyMonster(creature, out var monster))
        {
            return null;
        }

        if (creature.CombatState is not null && !ReferenceEquals(creature.CombatState, combatState))
        {
            Entry.Logger.Debug($"EnemyContext skipped detached creature: {creature.GetType().FullName}.");
            return null;
        }

        return new EnemyContext(combatState, creature, monster);
    }

    internal static bool TryGetEnemyMonster(Creature creature, out MonsterModel monster)
    {
        ArgumentNullException.ThrowIfNull(creature);

        if (creature is { IsMonster: true, IsEnemy: true, Monster: { } model })
        {
            monster = model;
            return true;
        }

        monster = null!;
        return false;
    }

    private static ICombatState EnsureCombatState(ICombatState combatState, Creature creature)
    {
        ArgumentNullException.ThrowIfNull(combatState);
        ArgumentNullException.ThrowIfNull(creature);

        if (creature.CombatState is not null && !ReferenceEquals(creature.CombatState, combatState))
        {
            throw new ArgumentException("Creature belongs to a different combat state.", nameof(combatState));
        }

        return combatState;
    }

    private static MonsterModel EnsureEnemyMonster(Creature creature, MonsterModel monster)
    {
        ArgumentNullException.ThrowIfNull(creature);
        ArgumentNullException.ThrowIfNull(monster);

        if (!TryGetEnemyMonster(creature, out var creatureMonster))
        {
            throw new ArgumentException("Creature must be an enemy monster creature.", nameof(creature));
        }

        if (!ReferenceEquals(creatureMonster, monster))
        {
            throw new ArgumentException("Monster must match Creature.Monster.", nameof(monster));
        }

        return monster;
    }
}

/// <summary>
/// 敌人进入死亡判定前发布的上下文。
/// </summary>
public sealed record EnemyDyingContext
{
    /// <summary>
    /// 创建一个经过校验的敌人死亡前上下文。
    /// </summary>
    public EnemyDyingContext(IRunState runState, ICombatState combatState, Creature creature, MonsterModel monster)
    {
        ArgumentNullException.ThrowIfNull(runState);

        Enemy = new EnemyContext(combatState, creature, monster);
        RunState = runState;
    }

    /// <summary>
    /// 当前 run state。
    /// </summary>
    public IRunState RunState { get; }

    /// <summary>
    /// 敌人所属的战斗状态。
    /// </summary>
    public ICombatState CombatState => Enemy.CombatState;

    /// <summary>
    /// 正在进入死亡判定的 creature。
    /// </summary>
    public Creature Creature => Enemy.Creature;

    /// <summary>
    /// 与 creature 绑定的 monster model。
    /// </summary>
    public MonsterModel Monster => Enemy.Monster;

    internal EnemyContext Enemy { get; }

    /// <summary>
    /// 从 STS2 death hook 参数创建敌人死亡前上下文。
    /// </summary>
    internal static EnemyDyingContext? TryCreate(IRunState runState, ICombatState? combatState, Creature creature)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(creature);

        if (combatState is null)
        {
            Entry.Logger.Debug("EnemyDyingContext skipped creature without combat state.");
            return null;
        }

        var enemy = EnemyContext.TryCreate(combatState, creature);
        if (enemy is null)
        {
            return null;
        }

        return new EnemyDyingContext(runState, enemy.CombatState, enemy.Creature, enemy.Monster);
    }

    internal EnemyContext ToEnemyContext()
    {
        return Enemy;
    }
}

/// <summary>
/// 敌人死亡 hook 完成后发布的上下文。
/// </summary>
public sealed record EnemyDiedContext
{
    /// <summary>
    /// 创建一个经过校验的敌人死亡后上下文。
    /// </summary>
    public EnemyDiedContext(
        IRunState runState,
        ICombatState combatState,
        Creature creature,
        MonsterModel monster,
        bool wasRemovalPrevented,
        float deathAnimationDurationSeconds)
    {
        ArgumentNullException.ThrowIfNull(runState);

        Enemy = new EnemyContext(combatState, creature, monster);
        RunState = runState;
        WasRemovalPrevented = wasRemovalPrevented;
        DeathAnimationDurationSeconds = deathAnimationDurationSeconds;
    }

    /// <summary>
    /// 当前 run state。
    /// </summary>
    public IRunState RunState { get; }

    /// <summary>
    /// 敌人所属的战斗状态。
    /// </summary>
    public ICombatState CombatState => Enemy.CombatState;

    /// <summary>
    /// 已经进入死亡流程的 creature。
    /// </summary>
    public Creature Creature => Enemy.Creature;

    /// <summary>
    /// 与 creature 绑定的 monster model。
    /// </summary>
    public MonsterModel Monster => Enemy.Monster;

    /// <summary>
    /// STS2 是否阻止了移除该 creature。
    /// </summary>
    public bool WasRemovalPrevented { get; }

    /// <summary>
    /// STS2 传入的死亡动画时长（秒）。
    /// </summary>
    public float DeathAnimationDurationSeconds { get; }

    internal EnemyContext Enemy { get; }

    /// <summary>
    /// 从 STS2 death hook 参数创建敌人死亡后上下文。
    /// </summary>
    internal static EnemyDiedContext? TryCreate(
        IRunState runState,
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimationDurationSeconds)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(creature);

        if (combatState is null)
        {
            Entry.Logger.Debug("EnemyDiedContext skipped creature without combat state.");
            return null;
        }

        var enemy = EnemyContext.TryCreate(combatState, creature);
        if (enemy is null)
        {
            return null;
        }

        return new EnemyDiedContext(
            runState,
            enemy.CombatState,
            enemy.Creature,
            enemy.Monster,
            wasRemovalPrevented,
            deathAnimationDurationSeconds);
    }

    internal EnemyContext ToEnemyContext()
    {
        return Enemy;
    }
}
