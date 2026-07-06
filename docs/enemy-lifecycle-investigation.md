# Enemy Lifecycle 调查记录

本文记录 KoraxLib M1 enemy lifecycle events 的本地代码调查结论。它是实现前的依据，不替代 [spec.md](spec.md) 的 API 合同。

## 调查范围

- STS2 0.107.1 反编译源码：`/home/aspharos/Projects/sts2mod/Slay the Spire 2 Gdre/src`
- RitsuLib 生命周期 patch 参考：`/home/aspharos/Projects/sts2mod/STS2-RitsuLib/src/Lifecycle`
- KoraxLib 当前文档和源码：`docs/`、`src/`

## Hook 签名和时序

### EnemySpawned

来源：`Hook.AfterCreatureAddedToCombat(ICombatState combatState, Creature creature)`。

证据：

- `Core/Hooks/Hook.cs:354` 说明该 hook 只对战斗中途加入的 creature 直接分发，不走普通 combat hook guard。
- `Core/Hooks/Hook.cs:362` 签名为 `Task AfterCreatureAddedToCombat(ICombatState, Creature)`。
- `Core/Commands/CreatureCmd.cs:81` 在 `CreatureCmd.AddToCombat` 路径中 `await Hook.AfterCreatureAddedToCombat(creature.CombatState, creature)`。

结论：

- KoraxLib 应 patch 该 hook 的 postfix，并把原 hook 返回的 `Task` 接续到完成后再发布 `EnemySpawned`。
- 该 hook 不覆盖战斗开始时已经存在的初始怪物；实际实现额外 patch `CombatManager.SetUpCombat(CombatState)` postfix，对初始 `state.Creatures` 中的 enemy monster 补发 `EnemySpawned`。

### EnemyTurnStarting

来源：`Hook.BeforeSideTurnStart(ICombatState combatState, CombatSide side, IReadOnlyList<Creature> participants)`。

证据：

- `Core/Hooks/Hook.cs:1144` 签名为 `Task BeforeSideTurnStart(ICombatState, CombatSide, IReadOnlyList<Creature>)`。
- `Core/Combat/CombatManager.cs:444` 从 `CreaturesOnCurrentSide` 复制本次开始回合的 participants。
- `Core/Combat/CombatManager.cs:449` 先对 participants 调用 `BeforeTurnStart`。
- `Core/Combat/CombatManager.cs:458` 随后 `await Hook.BeforeSideTurnStart(...)`。

结论：

- KoraxLib 应使用 prefix 发布 `EnemyTurnStarting`，让订阅方观察原 hook 处理前的状态。
- 只对 `side == CombatSide.Enemy` 且 participants 中满足 enemy 过滤的 creature 发布。

### EnemyTurnStarted

来源：`Hook.AfterSideTurnStart(ICombatState combatState, CombatSide side, IReadOnlyList<Creature> participants)`。

证据：

- `Core/Hooks/Hook.cs:1163` 签名为 `Task AfterSideTurnStart(ICombatState, CombatSide, IReadOnlyList<Creature>)`。
- `Core/Combat/CombatManager.cs:492` 先对 participants 调用 `AfterTurnStart`。
- `Core/Combat/CombatManager.cs:520` 到 `522` 之后才 `await Hook.AfterSideTurnStart(...)`。
- `Core/Hooks/Hook.cs:1165` 到 `1174` 原 hook 内先运行 `AfterSideTurnStart`，再运行 `AfterSideTurnStartLate`。

结论：

- KoraxLib 应使用 postfix task bridge，在原 hook 完成后发布 `EnemyTurnStarted`。
- 这样事件语义是“STS2 的 side turn start hook 完整跑完后”，而不是“CombatManager 刚进入 after hook 时”。

### EnemyDying

来源：`Hook.BeforeDeath(IRunState runState, ICombatState? combatState, Creature creature)`。

证据：

- `Core/Hooks/Hook.cs:438` 签名为 `Task BeforeDeath(IRunState, ICombatState?, Creature)`。
- `Core/Commands/CreatureCmd.cs:503` 在 `ShouldDie` 判断前 `await Hook.BeforeDeath(runState, combatState, creature)`。
- `Core/Commands/CreatureCmd.cs:505` 随后才检查 `Hook.ShouldDie(...)`。

结论：

- KoraxLib 应使用 prefix 发布 `EnemyDying`，语义是“死亡流程开始，尚未确认是否会被阻止”。
- `combatState` 在 STS2 签名中可空；KoraxLib 的 enemy-only 事件可以要求非空 combat state，无法构造 `EnemyContext` 时跳过并记录 debug。

### EnemyDied

来源：`Hook.AfterDeath(IRunState runState, ICombatState? combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)`。

证据：

- `Core/Hooks/Hook.cs:450` 签名为 `Task AfterDeath(IRunState, ICombatState?, Creature, bool, float)`。
- `Core/Commands/CreatureCmd.cs:519` 正常死亡路径调用 `AfterDeath(..., wasRemovalPrevented: false, deathAnimLength)`。
- `Core/Commands/CreatureCmd.cs:566` 防死路径调用 `AfterDeath(..., wasRemovalPrevented: true, 0f)`。
- `Core/Commands/CreatureCmd.cs:523` 到 `530` 在 `AfterDeath` 之后才从 `CombatManager` / `CombatState` 移除 enemy creature。

结论：

- KoraxLib 应使用 postfix task bridge，在原 hook 完成后发布 `EnemyDied`。
- `EnemyDiedContext.WasRemovalPrevented` 必须保留；为 `true` 时 creature 可能仍然处于可恢复或递归死亡处理路径。
- 事件发布时 creature 通常尚未从 enemy list 移除，因此订阅方不能把 `EnemyDied` 理解为“已经移出战斗”。

## Enemy 过滤规则

证据：

- `Core/Entities/Creatures/Creature.cs:165`：`Creature.IsMonster => Monster != null`。
- `Core/Entities/Creatures/Creature.cs:243`：`Creature.IsEnemy => Side == CombatSide.Enemy`。
- `Core/Entities/Creatures/Creature.cs:252` 到 `260`：primary enemy 只对 enemy side 成立。

结论：

- KoraxLib enemy context 应要求 `creature.IsMonster && creature.IsEnemy && creature.Monster != null`。
- `EnemyTurnStarting` / `EnemyTurnStarted` 应遍历 hook participants，并只发布给满足上述条件的 creature。
- 不应默认只发布给 primary enemy；secondary enemy / summon 仍然是 enemy lifecycle 的一部分。

## Patch 时序建议

RitsuLib 提供了成熟参考：

- `BeforeSideTurnStartLifecyclePatch` 使用 prefix 发布 side-turn-starting event（`STS2-RitsuLib/src/Lifecycle/Patches/CombatHookLifecyclePatches.cs:141`）。
- `AfterSideTurnStartLifecyclePatch` 使用 postfix，并通过 `LifecyclePatchTaskBridge.After(__result, ...)` 接续原 hook task（同文件 `:169` 到 `:175`）。
- `BeforeDeathLifecyclePatch` 使用 prefix 发布 dying event（同文件 `:420` 到 `:425`）。
- `AfterDeathLifecyclePatch` 使用 postfix task bridge 发布 died event（同文件 `:444` 到 `:456`）。
- `AfterCreatureAddedToCombatLifecyclePatch` 使用 postfix task bridge 发布 added-to-combat event（`AdditionalHookLifecycleAfterPatches.cs:35` 到 `:41`）。
- `LifecyclePatchTaskBridge` 只是薄包装，最终委托给 `HarmonyAsyncTaskBridge.After`（`LifecyclePatchTaskBridge.cs:5` 到 `:20`）。

STSVWB 也证明普通内容模组可以直接使用 `[HarmonyPatch(typeof(Hook))]`：

- `STSVWB/src/Powers/Patches/SkyBoundArtTurnStartPatch.cs:10` 到 `:15` 直接 postfix `Hook.AfterPlayerTurnStart`。
- `STSVWB/src/Core/Tracking/Patches/TurnDamageTrackerPatch.cs:13` 到 `:18` 直接 prefix `Hook.BeforeDamageReceived`。

因此 KoraxLib 可以保持轻量，继续使用自身 `Harmony.PatchAll` 扫描 internal patch 类，不需要引入 RitsuLib 的 patch registry。

KoraxLib 不应该依赖 RitsuLib，但可以复用这个设计原则：

- `Before*`：prefix 立即发布，保持原 hook 前语义。
- `After*`：postfix 替换 `ref Task __result`，把 KoraxLib 分发接到原 task 后面。
- 分发器内部按订阅顺序 await；单个 handler 失败时记录日志并继续后续 handler。
- RitsuLib 的 `HarmonyAsyncTaskBridge` 会重新抛 continuation 异常（`HarmonyAsyncTaskBridge.cs:117` 到 `:154`）；KoraxLib 若要满足“记录并继续”的 SPEC，不能直接复制其异常传播语义，应该在 KoraxLib dispatcher 内逐 handler 捕获。

## 需要在实现时新增的内部组件

- `src/Enemies/EnemyContext.cs`
- `src/Enemies/EnemyEvents.cs`
- `src/Enemies/EnemyPluginRegistry.cs`
- `src/Enemies/IEnemyPlugin.cs`
- `src/Internal/Patching/EnemyLifecyclePatches.cs`
- `src/Internal/Patching/HookTaskBridge.cs` 或等价内部 helper
- 可选：`src/Internal/Smoke/EnemyLifecycleSmokeLogger.cs`，由环境变量启用。

## 验收建议

1. 用 `scripts/run-smoke-linux.sh` 启动，并用 console 触发 `KoraxSmokeEncounter`。
2. 启用 lifecycle smoke logger。
3. 日志至少应出现：`EnemySpawned`、`EnemyTurnStarting`、`EnemyTurnStarted`、`EnemyDying`、`EnemyDied`。
4. 普通击杀路径应记录 `WasRemovalPrevented=false`。
5. 如果能构造防死效果，再验证 `WasRemovalPrevented=true` 路径；M1 可先把它列为后续 QA。

## 当前未决点

- `AfterDeath` 内部会在无 `LocalContext.NetId` 时直接 return。KoraxLib postfix 是否仍应在这种情况下发布，需要实现时基于实际单机/测试环境验证。
- 是否需要公开 `EnemyTurnStarting` 的 side/participants 信息，还是每个 enemy context 保持最小字段，留给插件通过 `CombatState` 查询。

## 运行时 smoke 发现

- 实测 `KoraxSmokeEncounter` 中原版 `Nibbit` 死亡时已出现 `EnemyDying` / `EnemyDied` 日志，death hook 链路可用。
- 实测保存 current run 时曾出现 `Unknown ModelId entry 'ENCOUNTER.KORAX_SMOKE_ENCOUNTER' during serialization, writing NONE`，根因是 STS2 `ModelIdSerializationCache.Init()` 的 subtype 扫描没有拾取该 registered encounter entry；已通过 `ModelIdSerializationCacheRegistrationPatches` 在 init postfix 补入 registered encounter ids。
