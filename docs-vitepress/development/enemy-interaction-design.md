# Enemy Interaction Design

This page turns the STSVWB Achim investigation into an executable KoraxLib roadmap. The design goal is not just to expose STS2 internals; it is to let mod authors work with vanilla enemies through stable, high-level verbs.

## Target Experience

Mod authors should be able to write code close to the card text:

```csharp
await EnemyPowerFlow.StealRandomBuff(enemy, Owner.Creature, sourceCard: this);
await EnemyPowerFlow.InheritDebuffs(fromEnemy, toEnemy);
EnemyIntentInfo intent = EnemyIntentService.Get(enemy);
```

Advanced authors can drop to lower layers (`PowerTransferCatalogue`, adapters, raw STS2 commands), but common card effects should not require manually reading `monster.NextMove.Intents`, maintaining power class-name blacklists, or touching `MonsterMoveStateMachine.StateLog`.

## Layer Model

| Layer | Purpose | Current Status |
| --- | --- | --- |
| `PowerTransfer` | Classify and transfer enemy powers safely. | Implemented for `SafeClone`; adapters are registerable. |
| `EnemyPowerFlow` | High-level verbs: steal, inherit, convert to card, remove. | Planned. |
| `EnemyIntentService` | Read enemy intents without inspecting `NextMove.Intents` directly. | Planned. |
| `EnemyIntentOverride` | Controlled move replacement such as sleep/delay/force next move. | Deferred; high risk. |
| `EnemySnapshot` | One stable view of powers, intent, tags, enemy role, and transferability. | Planned. |

## Executable Phases

### Phase 1: Power Transfer Foundation

Status: implemented.

- `PowerTransferCatalogue` classifies known vanilla powers.
- Unknown powers default to `Unsupported`.
- `PowerTransferService.TransferAsync` executes `SafeClone` powers.
- `PowerTransferAdapterRegistry` routes `NeedsAdapter` powers to consumer-provided adapters.

Exit criteria before expanding:

- Add one in-game smoke card that steals a known `SafeClone` buff.
- Verify source removal, target application, combat continuation, and save stability.

### Phase 2: EnemyPowerFlow Convenience API

Goal: make Achim-like cards short and readable.

Planned API shape:

```csharp
await EnemyPowerFlow
    .From(enemy)
    .WhereBuff()
    .PickRandom(Owner.RunState.Rng.CombatTargets)
    .TransferTo(Owner.Creature, sourceCard: this);
```

Required work:

- Add filter helpers for buff/debuff/visible/amount-positive powers.
- Add deterministic random selection via caller-supplied STS2 RNG.
- Return structured results for no candidates, adapter required, unsupported, and applied.
- Keep source removal explicit (`Steal`) vs copying explicit (`Copy`).

### Phase 3: Built-In Adapter Samples

Goal: prove the adapter system is useful for real enemy-only powers.

First candidates:

| Power | Reason |
| --- | --- |
| `CurlUpPower` | Small scope; original writes `LouseProgenitor.Curled`. |
| `SkittishPower` | Common enemy-side block trigger with per-turn state. |
| `StockPower` | Counter-only player-side replacement is straightforward. |

Do this only after KoraxLib can safely register custom power models or apply consumer-provided replacement powers.

### Phase 4: Enemy Intent Read API

Goal: replace direct `monster.NextMove.Intents` inspection.

Planned API shape:

```csharp
EnemyIntentInfo info = EnemyIntentService.Get(enemy);
bool attacks = info.IntendsToAttack;
int damage = info.AttackDamage;
```

Required work:

- Wrap current intent lookup and attack damage calculation.
- Return neutral `Unknown` when the move is not readable.
- Add examples for “if enemy attacks for 0, apply a debuff” effects.

### Phase 5: Enemy Intent Override

Goal: replace direct `SetMoveImmediate` and state-log manipulation.

This is intentionally last. It touches the most fragile STS2 enemy internals.

Planned API shape:

```csharp
await EnemyIntentOverride.SleepThisTurn(enemy);
await EnemyIntentOverride.DelayOneTurn(enemy);
```

Required work:

- Preserve follow-up state safely.
- Avoid corrupting state logs.
- Smoke test against at least one simple enemy, one conditional enemy, and one random-branch enemy.

## Current Boundary

KoraxLib currently supports safe power-transfer decisions and safe-clone execution. It does not yet provide the full fluent `EnemyPowerFlow`, `EnemyIntentService`, or move override API.

Docs and examples should keep this distinction explicit: do not show planned high-level APIs as available runtime surfaces until source files exist.
