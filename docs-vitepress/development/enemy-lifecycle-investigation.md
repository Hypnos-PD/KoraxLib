# Enemy Lifecycle Investigation

This page summarizes `docs/enemy-lifecycle-investigation.md`.

## Findings

- `Hook.AfterCreatureAddedToCombat` covers enemies added during combat, but not initial encounter enemies.
- `CombatManager.SetUpCombat` is patched to publish `EnemySpawned` for initial enemies.
- Enemy turn events are based on STS2 side-turn hooks and hook participants.
- Death events are based on `Hook.BeforeDeath` and `Hook.AfterDeath`.
- Enemy contexts require enemy-side monster creatures.

## Runtime Smoke Result

The latest manual smoke run verified:

```text
[KoraxLib] [LifecycleSmoke] EnemySpawned: MonsterType=Nibbit, CombatId=1, CurrentHp=46
[KoraxLib] [LifecycleSmoke] EnemyDying: MonsterType=Nibbit, CombatId=1, CurrentHp=0
[KoraxLib] [LifecycleSmoke] EnemyDied: MonsterType=Nibbit, CombatId=1, CurrentHp=0, WasRemovalPrevented=False, DeathAnimationDurationSeconds=0.86666673
```

It also verified `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` victory and save serialization without the previous unknown model ID entry error.
