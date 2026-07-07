# 敌人生命周期调查

本页概述 `docs/enemy-lifecycle-investigation.md`。

## 结论

- `Hook.AfterCreatureAddedToCombat` 覆盖战斗中途加入的 enemies，但不覆盖初始 encounter enemies。
- `CombatManager.SetUpCombat` 已被 patch，用于给初始 enemies 发布 `EnemySpawned`。
- 敌方回合事件基于 STS2 side-turn hooks 和 hook participants。
- 死亡事件基于 `Hook.BeforeDeath` 和 `Hook.AfterDeath`。
- Enemy contexts 要求 enemy-side monster creatures。

## 运行时 Smoke 结果

最新手动 smoke 已验证：

```text
[KoraxLib] [LifecycleSmoke] EnemySpawned: MonsterType=Nibbit, CombatId=1, CurrentHp=46
[KoraxLib] [LifecycleSmoke] EnemyDying: MonsterType=Nibbit, CombatId=1, CurrentHp=0
[KoraxLib] [LifecycleSmoke] EnemyDied: MonsterType=Nibbit, CombatId=1, CurrentHp=0, WasRemovalPrevented=False, DeathAnimationDurationSeconds=0.86666673
```

同时验证了 `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` 胜利结算和存档序列化，不再出现之前的 unknown model ID entry 错误。
