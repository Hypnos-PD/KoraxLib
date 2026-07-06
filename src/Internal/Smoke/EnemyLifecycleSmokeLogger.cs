using KoraxLib.Enemies;

namespace KoraxLib.Internal.Smoke;

/// <summary>
/// 通过环境变量开启的敌人生命周期 smoke 日志，用于游戏内手动验证 hook 链路。
/// </summary>
internal static class EnemyLifecycleSmokeLogger
{
    private const string EnableSmokeEnvironmentVariable = "KORAXLIB_ENABLE_LIFECYCLE_SMOKE";

    private static readonly object SyncRoot = new();
    private static bool _enabled;

    /// <summary>
    /// 当 <c>KORAXLIB_ENABLE_LIFECYCLE_SMOKE=1</c> 时订阅所有敌人生命周期事件。
    /// </summary>
    internal static void EnableIfRequested()
    {
        if (Environment.GetEnvironmentVariable(EnableSmokeEnvironmentVariable) != "1")
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_enabled)
            {
                return;
            }

            EnemyEvents.EnemySpawned += LogEnemySpawnedAsync;
            EnemyEvents.EnemyTurnStarting += LogEnemyTurnStartingAsync;
            EnemyEvents.EnemyTurnStarted += LogEnemyTurnStartedAsync;
            EnemyEvents.EnemyDying += LogEnemyDyingAsync;
            EnemyEvents.EnemyDied += LogEnemyDiedAsync;
            _enabled = true;
        }

        Entry.Logger.Info("KoraxLib enemy lifecycle smoke logger enabled.");
    }

    private static Task LogEnemySpawnedAsync(EnemyContext context)
    {
        Entry.Logger.Info($"[LifecycleSmoke] EnemySpawned: {Describe(context)}");
        return Task.CompletedTask;
    }

    private static Task LogEnemyTurnStartingAsync(EnemyContext context)
    {
        Entry.Logger.Info($"[LifecycleSmoke] EnemyTurnStarting: {Describe(context)}");
        return Task.CompletedTask;
    }

    private static Task LogEnemyTurnStartedAsync(EnemyContext context)
    {
        Entry.Logger.Info($"[LifecycleSmoke] EnemyTurnStarted: {Describe(context)}");
        return Task.CompletedTask;
    }

    private static Task LogEnemyDyingAsync(EnemyDyingContext context)
    {
        Entry.Logger.Info($"[LifecycleSmoke] EnemyDying: {Describe(context.ToEnemyContext())}");
        return Task.CompletedTask;
    }

    private static Task LogEnemyDiedAsync(EnemyDiedContext context)
    {
        Entry.Logger.Info(
            $"[LifecycleSmoke] EnemyDied: {Describe(context.ToEnemyContext())}, " +
            $"WasRemovalPrevented={context.WasRemovalPrevented}, " +
            $"DeathAnimationDurationSeconds={context.DeathAnimationDurationSeconds}");
        return Task.CompletedTask;
    }

    private static string Describe(EnemyContext context)
    {
        return $"MonsterType={context.Monster.GetType().Name}, CombatId={context.Creature.CombatId?.ToString() ?? "none"}, CurrentHp={context.Creature.CurrentHp}";
    }
}
