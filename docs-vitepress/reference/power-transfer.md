# Power Transfer

`KoraxLib.Enemies.PowerTransfer` provides a shared compatibility catalogue for mods that inspect or move vanilla enemy powers.

This module is seeded from the compatibility work in STSVWB's `AchimPowerTransferMapper`: powers that can be cloned safely, powers that need a player-safe replacement, and powers that should not be transferred because they depend on enemy lifecycle or encounter state.

::: warning Adapter Path Not Implemented
`SafeClone` powers can now be transferred at runtime. `NeedsAdapter` powers still return `AdapterRequired` and do not modify combat state until runtime adapters are implemented.
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

`TransferAsync` executes only the `SafeClone` path. It validates that the source amount is positive and that the target can receive powers before calling STS2's `PowerCmd.Apply`. When `RemoveSource` is `true`, the source power is removed after the apply command is issued.

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

`TransferAsync` never applies `NeedsAdapter` or `Unsupported` powers. Those paths return structured results and leave combat state unchanged.

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

## Adapter Contract

`IPowerTransferAdapter` is metadata-only for now:

```csharp
public interface IPowerTransferAdapter
{
    string AdaptedPowerClassName { get; }
    string ReplacementPowerClassName { get; }
}
```

Runtime adapter creation is intentionally deferred. `IPowerTransferAdapter` records the shape of that future API without pretending replacement creation is available today.
