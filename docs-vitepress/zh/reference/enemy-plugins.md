# 敌人插件

`IEnemyPlugin` 把多个 lifecycle handlers 组织在一个对象里，并允许插件自己决定适用于哪些敌人。

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

## 注册插件

```csharp
EnemyPluginRegistry.Register(new MyEnemyPlugin());
```

重复注册同一 plugin 实例会被忽略。

## 过滤

每个 lifecycle handler 前都会调用 `AppliesTo`。如果返回 `false`，handler 会被跳过。如果 `AppliesTo` 抛错，KoraxLib 会记录日志，并把该 plugin 视为不适用于当前 context。

## 注销

```csharp
var removed = EnemyPluginRegistry.Unregister(plugin);
```

`Unregister` 返回该 plugin 实例是否存在。

## 直接事件还是插件

简单的一次性订阅可以直接用 `EnemyEvents`。如果多个 handlers 共享同一 enemy filter 或状态，使用 `IEnemyPlugin` 更合适。
