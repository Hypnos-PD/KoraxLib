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

## Next Implementation Area

The next large M1 area is vanilla ability registry, preview, executor skeleton, primitive abilities, and the first hand-written catalog entries.

## Deferred

- Large enemy DSL.
- Automatic assembly scanning.
- Full vanilla monster ability catalog.
- Visual catalog viewer.
- Steam or Workshop packaging tools.
- Runtime configuration UI.
- Hard dependency on RitsuLib internals.
