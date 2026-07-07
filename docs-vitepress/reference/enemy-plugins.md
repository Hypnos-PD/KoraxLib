# Enemy Plugins

`IEnemyPlugin` groups lifecycle handlers behind one object and lets the plugin decide which enemies it applies to.

```csharp
namespace KoraxLib.Enemies;

public interface IEnemyPlugin
{
    bool AppliesTo(EnemyContext context);

    Task OnSpawned(EnemyContext context) => Task.CompletedTask;
    Task OnTurnStarting(EnemyContext context) => Task.CompletedTask;
    Task OnTurnStarted(EnemyContext context) => Task.CompletedTask;
    Task OnDying(EnemyDyingContext context) => Task.CompletedTask;
    Task OnDied(EnemyDiedContext context) => Task.CompletedTask;
}
```

## Register A Plugin

```csharp
EnemyPluginRegistry.Register(new MyEnemyPlugin());
```

Duplicate registration of the same plugin instance is ignored.

## Filtering

`AppliesTo` runs before each lifecycle handler. If it returns `false`, the handler is skipped. If `AppliesTo` throws, KoraxLib logs the failure and treats the plugin as not applicable for that context.

## Unregister

```csharp
var removed = EnemyPluginRegistry.Unregister(plugin);
```

`Unregister` returns whether the plugin instance was present.

## Direct Events Or Plugins

Use `EnemyEvents` for simple one-off subscriptions. Use `IEnemyPlugin` when several handlers share the same enemy filter or state.
