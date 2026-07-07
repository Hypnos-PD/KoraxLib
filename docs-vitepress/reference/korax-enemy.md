# KoraxEnemy

`KoraxEnemy` is the recommended base class for creating custom enemies. It extends STS2's `MonsterModel` with a fluent move graph builder and mutable state helpers.

```csharp
namespace KoraxLib.Enemies.Behaviour;

public abstract class KoraxEnemy : MonsterModel
{
    protected abstract void ConfigureMoves(MoveGraph moves);

    protected void SetFlag(string key, object? value);
    protected T? GetFlag<T>(string key, T? defaultValue = default);
    protected bool HasFlag(string key);

    public static KoraxEnemy From(MonsterModel monster);
}
```

## ConfigureMoves

Override `ConfigureMoves` to define the enemy's move state machine using `MoveGraph`:

```csharp
protected override void ConfigureMoves(MoveGraph moves)
{
    moves.InitialState(when => when
        .When(creature => SomeCondition(creature), "moveA")
        .Else("moveB"));

    moves.Move("moveA", move => move
        .Intent(new SingleAttackIntent(6))
        .OnPerform(async targets => {
            await DamageCmd.Attack(6).FromMonster(this).Execute(null);
        })
        .Then("moveB"));

    moves.Move("moveB", move => move
        .Intent(new DefendIntent())
        .OnPerform(async targets => {
            await CreatureCmd.GainBlock(Creature, 5).Execute(null);
        })
        .Then("moveA"));
}
```

This method is called once per canonical instance. Each combat mutable clone builds a fresh `MonsterMoveStateMachine` from the same definition.

## SetFlag / GetFlag / HasFlag

Custom mutable state stored per clone. Useful for per-instance behaviour flags (front/back slot, solo detection, phase tracking).

```csharp
// In EncounterModel.GenerateMonsters():
var enemy = KoraxEnemy.From(myMutableClone);
enemy.SetFlag("isFront", true);

// In InitialState condition:
moves.InitialState(when => when
    .When(creature => ((MyEnemy)creature.Monster).HasFlag("isFront"), "attack")
    .Else("defend"));
```

Flags are reset to an empty dictionary in `AfterCloned()` — each mutable clone gets its own independent state.

## KoraxEnemy.From

Safely casts a `MonsterModel` to `KoraxEnemy`. Throws `InvalidOperationException` if the model is not a `KoraxEnemy` subclass.

## HP and Properties

`KoraxEnemy` does **not** abstract HP, SFX, visuals, or animation properties. Override them directly on your subclass exactly as you would with raw `MonsterModel`:

```csharp
public override int MinInitialHp =>
    AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

public override string DeathSfx =>
    "event:/sfx/enemy/enemy_attacks/my_enemy/my_enemy_die";
```

## GenerateMoveStateMachine

Automatically implemented by `KoraxEnemy`. Calls `MoveGraph.Build(this)` which produces a `MonsterMoveStateMachine` from the graph defined in `ConfigureMoves`. Marked `sealed override` — subclasses cannot override it.

## AfterCloned

Overridden to reset `_runtimeState` to a fresh dictionary per mutable clone. Subclasses that need additional clone cleanup should call `base.AfterCloned()`.

## See Also

- [EnemyRegistry](./enemy-registry) — type registration and encounter declaration
- [EnemyEvents](./enemy-events) — lifecycle hook events
- [Enemy Guide](../guide/enemy-guide) — full example with explanations
