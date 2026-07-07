# Enemy Guide

There are three complementary layers when working with enemies in KoraxLib:

- **Behaviour** — define what your enemy does (moves, intents, custom state). Use `KoraxEnemy`.
- **Registration** — tell STS2 about your enemy and its encounters. Use `EnemyRegistry`.
- **Interaction** — inspect or manipulate vanilla enemy powers safely. Use `PowerTransfer` today; see [Enemy Interaction Design](../development/enemy-interaction-design) for the planned higher-level API.

Use `KoraxEnemy` when you are authoring a new enemy. Use `PowerTransfer` when a card or relic needs to interact with existing vanilla enemies.

## Behaviour: Define An Enemy With KoraxEnemy

::: warning New API
`KoraxEnemy` is the recommended way to create enemies. The raw `MonsterModel` approach still works but requires more boilerplate.
:::

`KoraxEnemy` extends STS2's `MonsterModel` and provides a fluent move graph builder. Subclass it and implement `ConfigureMoves`:

```csharp
using KoraxLib.Enemies.Behaviour;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

public sealed class MyNibbit : KoraxEnemy
{
    // ── HP (inherit from MonsterModel) ─────────────────────

    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

    public override int MaxInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 48, 46);

    // ── SFX (inherit from MonsterModel) ────────────────────

    public override string DeathSfx =>
        "event:/sfx/enemy/enemy_attacks/nibbit/nibbit_die";

    // ── Moves ──────────────────────────────────────────────

    protected override void ConfigureMoves(MoveGraph moves)
    {
        // Ascension-scaled damage / block values
        var buttDmg  = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);
        var sliceDmg = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
        var sliceBlk = AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 6, 5);
        var hissStr  = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

        // Conditional initial state (like vanilla Nibbit's front/back logic)
        moves.InitialState(when => when
            .When(creature => ((MyNibbit)creature.Monster).HasFlag("isFront"), "slice")
            .Else("hiss"));

        // Moves form a deterministic cycle via .Then()
        moves.Move("butt", move => move
            .Intent(new SingleAttackIntent(buttDmg))
            .OnPerform(async targets =>
            {
                await DamageCmd.Attack(buttDmg)
                    .FromMonster(this)
                    .WithAttackerAnim("Attack", 0.15f)
                    .Execute(null);
            })
            .Then("slice"));

        moves.Move("slice", move => move
            .Intent(new SingleAttackIntent(sliceDmg))
            .Intent(new DefendIntent())
            .OnPerform(async targets =>
            {
                await DamageCmd.Attack(sliceDmg)
                    .FromMonster(this)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(null);
                await CreatureCmd.GainBlock(Creature, sliceBlk, ValueProp.Move, null);
            })
            .Then("hiss"));

        moves.Move("hiss", move => move
            .Intent(new BuffIntent())
            .OnPerform(async targets =>
            {
                await PowerCmd.Apply<Strength>(Creature, hissStr).Execute(null);
            })
            .Then("butt"));
    }
}
```

### Custom Mutable State

Use `SetFlag` / `GetFlag<T>` / `HasFlag` for per-clone runtime state, e.g. front/back position:

```csharp
// In your EncounterModel.GenerateMonsters():
var frontNibbit = ModelDb.Monster<MyNibbit>().ToMutable() as MyNibbit;
KoraxEnemy.From(frontNibbit).SetFlag("isFront", true);
```

Flags are scoped to the mutable clone — each combat instance gets its own dictionary (via `AfterCloned`).

### Move Graph Features

| Feature | API | Vanilla Equivalent |
| --- | --- | --- |
| Deterministic cycle | `.Move(id, ...).Then(next)` | `MoveState.FollowUpState` |
| Conditional initial state | `.InitialState(when => when.When(...))` | `ConditionalBranchState` |
| Weighted random branch | `.RandomBranch(id, ...)` | `RandomBranchState` |
| Multi-turn lock | `.RequireCompletion()` | `MoveState.MustPerformOnceBeforeTransitioning` |
| Multiple intents | `.Intent(...).Intent(...)` | `MoveState` constructor params |
| Cooldown control | `RandomBranchConfigurator.Branch(..., cooldown: 2)` | `RandomBranchState.AddBranch(cooldown)` |

## Registration: Tell STS2 About Your Content

Use `EnemyRegistry` during mod initialization to declare enemy-related content. KoraxLib records the declarations while registration is open, then applies them to STS2 model and encounter structures during the safe initialization phase.

### Register A Monster

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterMonster<MyNibbit>();
```

The type must be a concrete closed subtype of `MonsterModel`.

### Register An Act Encounter

```csharp
using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Models;

EnemyRegistry.RegisterActEncounter<Overgrowth, MyEncounterModel>();
```

Act-scoped encounters are appended after vanilla encounters for the target act. KoraxLib de-duplicates by `EncounterModel.Id`.

### Register A Global Encounter

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterGlobalEncounter<MyEncounterModel>();
```

Global encounters are appended to every supported act encounter list after act-scoped entries.

### Registration Lifecycle

`EnemyRegistry.State` exposes the current registration state:

```csharp
public enum KoraxRegistrationState
{
    Open,
    Frozen,
}
```

Register content while the state is `Open`. Once KoraxLib freezes registration, later content registration throws `InvalidOperationException`.

Runtime event subscription is separate from content registration and remains available after freeze.

### Duplicate Registrations

Registering the same type more than once is a no-op. KoraxLib logs the duplicate at debug level and keeps the first declaration.

### Save Serialization

KoraxLib patches `ModelIdSerializationCache.Init` so registered monster and encounter IDs are added to STS2's save/network serialization cache. The smoke encounter has verified that `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` can be saved without being written as `NONE`.
