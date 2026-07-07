# Vanilla Abilities

::: warning Planned API
Vanilla ability registry, preview, executor, primitive abilities, and catalog source files do not exist yet. This page records the intended direction so users do not mistake it for a supported runtime surface.
:::

KoraxLib intends to expose selected vanilla enemy behavior as neutral ability definitions. The design goal is to reuse behavior safely without calling `MonsterModel.PerformMove()` as a player-side ability API.

## Planned Concepts

| Concept | Purpose |
| --- | --- |
| `VanillaAbilityDefinition` | A named ability entry derived from reviewed vanilla behavior. |
| `VanillaAbilityRegistry` | A catalog of known definitions. |
| `VanillaAbilityPreview` | A no-mutation preview surface for UI and planning. |
| `VanillaAbilityExecutor` | A guarded execution entry point. |
| `VanillaAbilityRisk` | Risk classification: `Safe`, `ContextSensitive`, `Unsafe`. |

## Risk Model

- `Safe`: does not depend on original monster runtime state, encounter identity, private fields, or visuals.
- `ContextSensitive`: requires caller-supplied targets, source, RNG, or side policy.
- `Unsafe`: can be documented but should not execute through the normal executor.

## Current Guidance

Until this module exists, consumer mods should implement their own abilities directly or wait for the catalog work in later milestones.
