namespace KoraxLib.Enemies;

/// <summary>
/// 可订阅敌人生命周期事件的 KoraxLib 插件接口。
/// </summary>
public interface IEnemyPlugin
{
    /// <summary>
    /// 判断当前插件是否处理指定敌人上下文。
    /// </summary>
    bool AppliesTo(EnemyContext context);

    /// <summary>
    /// 敌人加入战斗后调用。
    /// </summary>
    Task OnSpawned(EnemyContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 敌方回合开始前调用。
    /// </summary>
    Task OnTurnStarting(EnemyContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 敌方回合开始 hook 完成后调用。
    /// </summary>
    Task OnTurnStarted(EnemyContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 敌人进入死亡判定前调用。
    /// </summary>
    Task OnDying(EnemyDyingContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 敌人死亡 hook 完成后调用。
    /// </summary>
    Task OnDied(EnemyDiedContext context)
    {
        return Task.CompletedTask;
    }
}
