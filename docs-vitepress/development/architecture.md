# Architecture Notes

This page summarizes `docs/architecture.md` for the documentation site.

## Goals

KoraxLib is a Slay the Spire 2 library mod for enemy-related systems and safer reuse of vanilla enemy behavior.

The architecture hides Harmony patches, `ModelDb` injection, and STS2 initialization order from consumer mods.

## Module Boundaries

- `Core`: registration lifecycle, diagnostics, patching infrastructure.
- `Enemies`: monster and encounter registration, lifecycle events, enemy plugin dispatch, enemy contexts.
- `VanillaAbilities`: planned module for reviewed vanilla behavior reuse.
- `MonsterMoves`: planned helpers for move-state authoring and review.
- `Visuals`: planned presentation helpers; visuals must not own combat rules.
- `Compatibility`: future adapters for external libraries.

## Dependency Direction

Core modules should not depend on enemy, ability, visual, or compatibility modules. Compatibility modules may depend on public KoraxLib modules but must not become core prerequisites.

## Public API Boundary

Supported public API should live under:

```text
KoraxLib.Enemies
KoraxLib.MonsterMoves
KoraxLib.VanillaAbilities
```

Internal implementation lives under `KoraxLib.Internal.*` and is not stable for consumers.
