# Power Transfer

`KoraxLib.Enemies.PowerTransfer` provides a shared compatibility catalogue for mods that inspect or move vanilla enemy powers.

This module is seeded from the compatibility work in STSVWB's `AchimPowerTransferMapper`: powers that can be cloned safely, powers that need a player-safe replacement, and powers that should not be transferred because they depend on enemy lifecycle or encounter state.

For the larger planned flow API (`EnemyPowerFlow`, `EnemyIntentService`, intent override), see [Enemy Interaction Design](../development/enemy-interaction-design).

::: warning Adapter Implementations Are Consumer-Owned
`SafeClone` powers can be transferred by KoraxLib directly. `NeedsAdapter` powers run only when a consuming mod registers an `IPowerTransferAdapter` for that power.
:::

## Entry Points

```csharp
using KoraxLib.Enemies.PowerTransfer;

PowerTransferSafety safety = PowerTransferService.ClassifyPower("SkittishPower");
bool canTransfer = PowerTransferService.CanTransfer("AfterimagePower");

if (PowerTransferService.TryGetEntry("CurlUpPower", out var entry))
{
    var adapterKey = entry.AdapterKey;
}
```

## Runtime Safe-Clone Transfer

`TransferAsync` validates that the source amount is positive and that the target can receive powers before touching combat state.

For `SafeClone`, it clones the source power, calls STS2's `PowerCmd.Apply`, then removes the source power when `RemoveSource` is `true`.

For `NeedsAdapter`, it looks up `PowerTransferAdapterRegistry`. If an adapter exists, the adapter executes the transfer. If no adapter exists, the result is `AdapterRequired`.

```csharp
PowerTransferResult result = await PowerTransferService.TransferAsync(new PowerTransferRequest
{
    ChoiceContext = choiceContext,
    SourcePower = randomBuff,
    Target = Owner.Creature,
    Applier = Owner.Creature,
    CardSource = this,
    RemoveSource = true,
});

if (result.Status == PowerTransferStatus.AdapterRequired)
{
    // Provide a compatible replacement before transferring this power.
}
```

`Unsupported` powers never run and leave combat state unchanged.

## PowerTransferStatus

| Value | Meaning |
| --- | --- |
| `Applied` | Safe-clone path was executed. |
| `AdapterRequired` | The power needs a compatible replacement implementation. |
| `Unsupported` | The power is classified as unsupported or unknown. |
| `EmptyAmount` | The source power amount is zero or negative. |
| `TargetCannotReceive` | The target cannot currently receive powers. |
| `CloneFailed` | `ClonePreservingMutability()` did not produce a `PowerModel`. |

## PowerTransferSafety

| Value | Meaning |
| --- | --- |
| `SafeClone` | The power can enter a generic clone path. |
| `NeedsAdapter` | The power is transferable only through a compatible replacement implementation. |
| `Unsupported` | The power should not be transferred. Unknown powers default here. |

## PowerTransferCatalogue

`PowerTransferCatalogue` owns the seed data and accepts extra entries while KoraxLib registration is still open:

```csharp
PowerTransferCatalogue.Register(
    PowerTransferEntry.NeedsAdapter("MyEnemyOnlyPower", "MY_ENEMY_ONLY_POWER"));
```

Known entries are keyed by simple class name, such as `CurlUpPower`. Fully qualified names also work for string lookup because KoraxLib normalizes to the final class segment.

## Seed Data

Current safe-clone seeds include powers from STSVWB's Achim allowlist, such as `AfterimagePower`, `BarricadePower`, `DexterityPower`, `PlatingPower`, `StormPower`, `VigorPower`, `OrbitPower`, and `HardenedShellPower`.

Current adapter-needed seeds:

| Power | Adapter Key | Reason |
| --- | --- | --- |
| `CurlUpPower` | `CURL_UP_POWER` | Vanilla implementation writes `LouseProgenitor.Curled`. |
| `SkittishPower` | `SKITTISH_POWER` | Vanilla implementation depends on enemy-side animation/state. |
| `StockPower` | `STOCK_POWER` | Vanilla stock behavior is tied to enemy death replacement logic. |

Unsupported seeds include enemy-lifecycle powers such as `HatchPower`, `InterceptPower`, `ReattachPower`, `SlumberPower`, `ThieveryPower`, and `VoidFormPower`.

## Adapter Registry

Register adapters while KoraxLib registration is open:

```csharp
PowerTransferAdapterRegistry.Register(new CurlUpTransferAdapter());
```

Duplicate adapter registrations for the same source power are ignored.

## Adapter Contract

`IPowerTransferAdapter` owns the replacement logic for one `NeedsAdapter` power:

```csharp
public interface IPowerTransferAdapter
{
    string AdaptedPowerClassName { get; }
    string ReplacementPowerClassName { get; }

    Task<PowerTransferResult> TransferAsync(
        PowerTransferRequest request,
        PowerTransferEntry entry);
}
```

Adapter code should apply its replacement power, respect `request.RemoveSource`, and return a `PowerTransferResult` that describes what happened.

## Current Boundary

Power Transfer is the low-level safe execution layer. It does not yet provide fluent selection helpers such as `StealRandomBuff`, inheritance helpers, power-to-card conversion, or enemy intent APIs. Those belong to the planned `EnemyPowerFlow` / `EnemyIntentService` layers.
