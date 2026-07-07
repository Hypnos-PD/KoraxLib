# EnemyEvents

`EnemyEvents` 提供敌人生命周期 hooks 的直接事件订阅入口。

```csharp
namespace KoraxLib.Enemies;

public static class EnemyEvents
{
    public static event Func<EnemyContext, Task>? EnemySpawned;
    public static event Func<EnemyContext, Task>? EnemyTurnStarting;
    public static event Func<EnemyContext, Task>? EnemyTurnStarted;
    public static event Func<EnemyDyingContext, Task>? EnemyDying;
    public static event Func<EnemyDiedContext, Task>? EnemyDied;
}
```

## Contexts

`EnemyContext` 为 enemy-only events 携带 combat state、creature 和 monster model。

`EnemyDyingContext` 为死亡流程开始前的事件增加 run state。

`EnemyDiedContext` 额外包含：

```csharp
public bool WasRemovalPrevented { get; }
public float DeathAnimationDurationSeconds { get; }
```

## 分发合同

- Handlers 按订阅顺序运行。
- 单个 handler 失败会被记录，不会阻止后续 handlers。
- 事件只为 enemy-side monster creatures 发布。
- Turn events 使用 STS2 hook participants，不重新扫描全部 enemies。

## 时序摘要

| 事件 | 时序 |
| --- | --- |
| `EnemySpawned` | enemy 加入 combat 后发布；初始 encounter enemies 由 combat setup postfix 覆盖。 |
| `EnemyTurnStarting` | STS2 enemy-side turn-start hook 主体前发布。 |
| `EnemyTurnStarted` | STS2 enemy-side turn-start hook task 完成后发布。 |
| `EnemyDying` | STS2 death prevention 判定前发布。 |
| `EnemyDied` | STS2 death hook task 完成后发布；combat removal 可能随后才发生。 |
