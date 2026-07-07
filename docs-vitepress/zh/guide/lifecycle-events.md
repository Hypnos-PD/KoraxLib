# 生命周期事件

KoraxLib 通过 `EnemyEvents` 和 `IEnemyPlugin` 暴露敌人生命周期事件。事件只面向 enemy：只有 monster 且位于 enemy side 的 creature 才会创建 context。

## 事件入口

```csharp
public static class EnemyEvents
{
    public static event Func<EnemyContext, Task>? EnemySpawned;
    public static event Func<EnemyContext, Task>? EnemyTurnStarting;
    public static event Func<EnemyContext, Task>? EnemyTurnStarted;
    public static event Func<EnemyDyingContext, Task>? EnemyDying;
    public static event Func<EnemyDiedContext, Task>? EnemyDied;
}
```

Handlers 按注册顺序运行。单个 handler 抛错时，KoraxLib 会记录日志并继续分发后续 handlers。

## EnemySpawned

`EnemySpawned` 在敌方 monster creature 加入战斗后发布。

KoraxLib 覆盖两条路径：

- `Hook.AfterCreatureAddedToCombat`：战斗开始后加入的 enemies。
- `CombatManager.SetUpCombat`：初始 encounter enemies，它们不会经过 `AfterCreatureAddedToCombat`。

Smoke test 已验证初始 encounter spawn：

```text
[KoraxLib] [LifecycleSmoke] EnemySpawned: MonsterType=Nibbit, CombatId=1, CurrentHp=46
```

## EnemyTurnStarting

`EnemyTurnStarting` 在 STS2 enemy-side turn-start hook 主体运行前发布。它只在 `side == CombatSide.Enemy` 时发布，并且只遍历 hook 传入的 enemy participants。

## EnemyTurnStarted

`EnemyTurnStarted` 在 STS2 enemy-side turn-start hook task 完成后发布。KoraxLib 不重新扫描全部 enemies；它使用 hook participants list。

## EnemyDying

`EnemyDying` 在 STS2 判断死亡是否被阻止前发布。Context 包含 `IRunState`、`ICombatState`、`Creature` 和 `MonsterModel`。

## EnemyDied

`EnemyDied` 在 STS2 death hook 完成后发布。它不保证 creature 已经从 combat state 移除。

```csharp
public sealed class EnemyDiedContext
{
    public bool WasRemovalPrevented { get; }
    public float DeathAnimationDurationSeconds { get; }
}
```

Smoke test 已验证普通死亡路径：

```text
[KoraxLib] [LifecycleSmoke] EnemyDying: MonsterType=Nibbit, CombatId=1, CurrentHp=0
[KoraxLib] [LifecycleSmoke] EnemyDied: MonsterType=Nibbit, CombatId=1, CurrentHp=0, WasRemovalPrevented=False, DeathAnimationDurationSeconds=0.86666673
```
