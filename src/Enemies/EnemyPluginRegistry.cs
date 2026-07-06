using System.Collections.ObjectModel;

namespace KoraxLib.Enemies;

/// <summary>
/// 管理 <see cref="IEnemyPlugin" /> 实例并把 <see cref="EnemyEvents" /> 分发给匹配插件。
/// </summary>
public static class EnemyPluginRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<IEnemyPlugin> Plugins = [];
    private static bool _initialized;

    /// <summary>
    /// 当前已注册插件的防御性快照。
    /// </summary>
    public static IReadOnlyCollection<IEnemyPlugin> RegisteredPlugins
    {
        get
        {
            lock (SyncRoot)
            {
                return new ReadOnlyCollection<IEnemyPlugin>(Plugins.ToArray());
            }
        }
    }

    /// <summary>
    /// 注册一个敌人生命周期插件；重复注册同一实例会被忽略。
    /// </summary>
    public static void Register(IEnemyPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (SyncRoot)
        {
            if (Plugins.Contains(plugin))
            {
                Entry.Logger.Debug($"EnemyPluginRegistry ignored duplicate plugin registration: {plugin.GetType().FullName}.");
                return;
            }

            Plugins.Add(plugin);
        }
    }

    /// <summary>
    /// 注销一个敌人生命周期插件。
    /// </summary>
    public static bool Unregister(IEnemyPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (SyncRoot)
        {
            return Plugins.Remove(plugin);
        }
    }

    internal static void Initialize()
    {
        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            EnemyEvents.EnemySpawned += DispatchSpawnedAsync;
            EnemyEvents.EnemyTurnStarting += DispatchTurnStartingAsync;
            EnemyEvents.EnemyTurnStarted += DispatchTurnStartedAsync;
            EnemyEvents.EnemyDying += DispatchDyingAsync;
            EnemyEvents.EnemyDied += DispatchDiedAsync;
            _initialized = true;
        }
    }

    private static Task DispatchSpawnedAsync(EnemyContext context)
    {
        return DispatchAsync(context, static plugin => plugin.OnSpawned);
    }

    private static Task DispatchTurnStartingAsync(EnemyContext context)
    {
        return DispatchAsync(context, static plugin => plugin.OnTurnStarting);
    }

    private static Task DispatchTurnStartedAsync(EnemyContext context)
    {
        return DispatchAsync(context, static plugin => plugin.OnTurnStarted);
    }

    private static Task DispatchDyingAsync(EnemyDyingContext context)
    {
        return DispatchAsync(context.ToEnemyContext(), plugin => plugin.OnDying(context));
    }

    private static Task DispatchDiedAsync(EnemyDiedContext context)
    {
        return DispatchAsync(context.ToEnemyContext(), plugin => plugin.OnDied(context));
    }

    private static async Task DispatchAsync(
        EnemyContext filterContext,
        Func<IEnemyPlugin, Func<EnemyContext, Task>> handlerSelector)
    {
        foreach (var plugin in RegisteredPlugins)
        {
            if (!PluginApplies(plugin, filterContext))
            {
                continue;
            }

            try
            {
                await handlerSelector(plugin)(filterContext);
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"Enemy plugin {plugin.GetType().FullName} failed: {ex}");
            }
        }
    }

    private static async Task DispatchAsync(EnemyContext filterContext, Func<IEnemyPlugin, Task> handlerSelector)
    {
        foreach (var plugin in RegisteredPlugins)
        {
            if (!PluginApplies(plugin, filterContext))
            {
                continue;
            }

            try
            {
                await handlerSelector(plugin);
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"Enemy plugin {plugin.GetType().FullName} failed: {ex}");
            }
        }
    }

    private static bool PluginApplies(IEnemyPlugin plugin, EnemyContext context)
    {
        try
        {
            return plugin.AppliesTo(context);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Enemy plugin {plugin.GetType().FullName} AppliesTo failed: {ex}");
            return false;
        }
    }
}
