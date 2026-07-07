# Lifecycle Events

KoraxLib exposes enemy lifecycle events through `EnemyEvents` and `IEnemyPlugin`. Events are enemy-only: contexts are created only for creatures that are monsters and on the enemy side.

## Event Surface

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

Handlers run in registration order. If a handler throws, KoraxLib logs the failure and continues dispatching later handlers.

## EnemySpawned

`EnemySpawned` is published after an enemy monster creature enters combat.

KoraxLib covers two paths:

- `Hook.AfterCreatureAddedToCombat` for enemies added after combat starts.
- `CombatManager.SetUpCombat` for initial encounter enemies that do not pass through `AfterCreatureAddedToCombat`.

The smoke test verified initial encounter spawn with:

```text
[KoraxLib] [LifecycleSmoke] EnemySpawned: MonsterType=Nibbit, CombatId=1, CurrentHp=46
```

## EnemyTurnStarting

`EnemyTurnStarting` is published before STS2's enemy-side turn-start hook body runs. It is only published for `side == CombatSide.Enemy` and only for enemy participants passed to the hook.

## EnemyTurnStarted

`EnemyTurnStarted` is published after STS2's enemy-side turn-start hook task completes. KoraxLib does not rescan all enemies; it uses the hook participants list.

## EnemyDying

`EnemyDying` is published before STS2 decides whether death is prevented. The context includes `IRunState`, `ICombatState`, `Creature`, and `MonsterModel`.

## EnemyDied

`EnemyDied` is published after STS2's death hook completes. It does not guarantee that the creature has already been removed from combat state.

```csharp
public sealed class EnemyDiedContext
{
    public bool WasRemovalPrevented { get; }
    public float DeathAnimationDurationSeconds { get; }
}
```

The smoke test verified the normal death path with:

```text
[KoraxLib] [LifecycleSmoke] EnemyDying: MonsterType=Nibbit, CombatId=1, CurrentHp=0
[KoraxLib] [LifecycleSmoke] EnemyDied: MonsterType=Nibbit, CombatId=1, CurrentHp=0, WasRemovalPrevented=False, DeathAnimationDurationSeconds=0.86666673
```
