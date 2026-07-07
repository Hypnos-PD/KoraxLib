# API Overview

KoraxLib's public API currently lives under `KoraxLib.Enemies` and `KoraxLib.Core.Registration`.

## Stable Public Namespaces

```text
KoraxLib.Enemies
KoraxLib.Core.Registration
```

Use these namespaces from consumer mods.

## Internal Namespace

```text
KoraxLib.Internal.*
```

::: warning Internal API
Types under `KoraxLib.Internal.*` exist to bridge STS2, Harmony, Godot, and smoke-test infrastructure. Consumer mods should not depend on them. They can change without compatibility guarantees.
:::

## Current Modules

| Area | Status | Entry Points |
| --- | --- | --- |
| Enemy registration | Implemented | `EnemyRegistry` |
| Enemy lifecycle events | Implemented | `EnemyEvents`, `IEnemyPlugin`, `EnemyPluginRegistry` |
| Enemy power transfer classification | Implemented | `PowerTransferService`, `PowerTransferCatalogue` |
| Registration lifecycle | Implemented | `KoraxRegistrationState` |
| Vanilla abilities | Planned | Not implemented in source yet |

## Verification Status

The current enemy path has been verified with in-game smoke testing:

- Initial encounter `EnemySpawned` for `Nibbit`.
- `EnemyDying` and `EnemyDied` on normal death.
- `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` victory and save serialization.
- No `Unknown ModelId entry 'ENCOUNTER.KORAX_SMOKE_ENCOUNTER'` during save.
