---
layout: home
title: KoraxLib
titleTemplate: Slay the Spire 2 Enemy Modding Library

hero:
  name: KoraxLib
  text: Enemy-focused modding primitives for Slay the Spire 2
  tagline: Register enemy content, observe lifecycle events, and build toward safer reuse of vanilla enemy behavior.
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: API Reference
      link: /reference/api-overview

features:
  - title: Enemy Registration
    details: Declare MonsterModel and EncounterModel types while KoraxLib handles STS2 ModelDb and encounter list integration.
  - title: Lifecycle Events
    details: Subscribe to spawned, turn-starting, turn-started, dying, and died events without patching STS2 hooks yourself.
  - title: Smoke-Tested Runtime Path
    details: The internal smoke encounter verifies initial spawn, death lifecycle, encounter victory, and save serialization behavior.
---

::: warning Early API
KoraxLib is still in the Milestone 1 phase. The enemy registry and lifecycle event surface exists; vanilla ability APIs are planned but not implemented yet.
:::
