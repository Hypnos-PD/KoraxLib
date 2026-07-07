# EnemyEvents

`EnemyEvents` exposes direct event subscription for enemy lifecycle hooks.

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

`EnemyContext` carries the combat state, creature, and monster model for enemy-only events.

`EnemyDyingContext` adds run state for death flow before removal prevention is known.

`EnemyDiedContext` adds:

```csharp
public bool WasRemovalPrevented { get; }
public float DeathAnimationDurationSeconds { get; }
```

## Dispatch Contract

- Handlers run in subscription order.
- One handler failure is logged and does not stop later handlers.
- Events are only published for enemy-side monster creatures.
- Turn events use STS2 hook participants, not a fresh scan of all enemies.

## Timing Summary

| Event | Timing |
| --- | --- |
| `EnemySpawned` | After enemy is added to combat; initial encounter enemies are covered by a combat setup postfix. |
| `EnemyTurnStarting` | Before STS2 enemy-side turn-start hook body. |
| `EnemyTurnStarted` | After STS2 enemy-side turn-start hook task completes. |
| `EnemyDying` | Before STS2 death prevention decision. |
| `EnemyDied` | After STS2 death hook task completes; removal from combat may happen later. |
