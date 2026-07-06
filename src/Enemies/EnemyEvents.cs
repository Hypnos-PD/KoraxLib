namespace KoraxLib.Enemies;

/// <summary>
/// KoraxLib 暴露给消费模组的敌人生命周期事件入口。
/// </summary>
public static class EnemyEvents
{
    /// <summary>
    /// 敌方 monster creature 被加入战斗后发布。
    /// </summary>
    public static event Func<EnemyContext, Task>? EnemySpawned;

    /// <summary>
    /// 敌方回合开始前，对 hook participants 中的每个敌人发布。
    /// </summary>
    public static event Func<EnemyContext, Task>? EnemyTurnStarting;

    /// <summary>
    /// 敌方回合开始 hook 完成后，对 hook participants 中的每个敌人发布。
    /// </summary>
    public static event Func<EnemyContext, Task>? EnemyTurnStarted;

    /// <summary>
    /// 敌人进入 STS2 死亡判定前发布。
    /// </summary>
    public static event Func<EnemyDyingContext, Task>? EnemyDying;

    /// <summary>
    /// STS2 死亡 hook 完成后发布；并不保证 creature 已经被移出战斗。
    /// </summary>
    public static event Func<EnemyDiedContext, Task>? EnemyDied;

    internal static Task DispatchEnemySpawnedAsync(EnemyContext context)
    {
        return DispatchAsync(EnemySpawned, context, nameof(EnemySpawned));
    }

    internal static Task DispatchEnemyTurnStartingAsync(EnemyContext context)
    {
        return DispatchAsync(EnemyTurnStarting, context, nameof(EnemyTurnStarting));
    }

    internal static Task DispatchEnemyTurnStartedAsync(EnemyContext context)
    {
        return DispatchAsync(EnemyTurnStarted, context, nameof(EnemyTurnStarted));
    }

    internal static Task DispatchEnemyDyingAsync(EnemyDyingContext context)
    {
        return DispatchAsync(EnemyDying, context, nameof(EnemyDying));
    }

    internal static Task DispatchEnemyDiedAsync(EnemyDiedContext context)
    {
        return DispatchAsync(EnemyDied, context, nameof(EnemyDied));
    }

    private static async Task DispatchAsync<TContext>(Func<TContext, Task>? handlers, TContext context, string eventName)
    {
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<TContext, Task>>())
        {
            try
            {
                await handler(context);
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"KoraxLib {eventName} handler failed: {ex}");
            }
        }
    }
}
