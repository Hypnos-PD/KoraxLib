# Development Roadmap

This page summarizes the repository roadmap. The source planning document remains `docs/roadmap.md`.

## Current Status

M1 enemy registration and lifecycle infrastructure is partially complete:

- Registration lifecycle skeleton exists.
- `EnemyRegistry` exists for monster and encounter declarations.
- Registered monsters are merged into STS2 `ModelDb`.
- Registered encounters are merged into act encounter lists.
- Registered monster and encounter IDs are added to `ModelIdSerializationCache`.
- Enemy lifecycle events and plugin dispatch are implemented.
- The internal smoke encounter has verified initial spawn, death lifecycle, victory, and save serialization.
- `KoraxEnemy` provides a fluent move graph base class for custom enemy behaviour.
- `PowerTransfer` provides vanilla enemy power classification, safe-clone transfer, and consumer-registered adapters.

## Next Implementation Area

The next implementation area is the higher-level enemy interaction layer described in [Enemy Interaction Design](./enemy-interaction-design):

1. Add a smoke/demo card that verifies `PowerTransferService.TransferAsync` in game.
2. Build `EnemyPowerFlow` convenience APIs for stealing, copying, inheriting, and removing enemy powers.
3. Add read-only `EnemyIntentService` before attempting any move override API.

## Deferred

- Large enemy DSL.
- Automatic assembly scanning.
- Full vanilla monster ability catalog.
- Enemy move override APIs that manipulate `MonsterMoveStateMachine`.
- Visual catalog viewer.
- Steam or Workshop packaging tools.
- Runtime configuration UI.
- Hard dependency on RitsuLib internals.
