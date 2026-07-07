# Milestone 1 Spec Notes

This page summarizes `docs/spec.md`. Treat the source spec as the implementation contract until M1 stabilizes.

## Implemented Areas

- Registration lifecycle: `Open` to `Frozen`.
- `EnemyRegistry` public registration API.
- Registered monster and encounter integration with STS2 model and encounter lists.
- Registered model ID serialization cache patch.
- Enemy contexts.
- Enemy lifecycle events.
- Enemy plugin registration and dispatch.

## Planned Areas

- Vanilla ability registry.
- Vanilla ability preview.
- Vanilla ability executor.
- Primitive abilities.
- First hand-written vanilla ability catalog.

## Testing Expectations

M1 expects unit tests where practical and at least one runtime smoke test for user-visible behavior. The current verified runtime smoke path covers the smoke encounter, lifecycle logs, victory, and save serialization.
