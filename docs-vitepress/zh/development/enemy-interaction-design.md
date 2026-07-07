# 敌人交互设计

本页把 STSVWB 中 Achim 的调查结果整理成 KoraxLib 可执行路线图。设计目标不是简单暴露 STS2 内部对象，而是让模组作者通过稳定的高层动词操作原版敌人。

## 目标体验

模组作者应该能写出接近卡牌文本的代码：

```csharp
await EnemyPowerFlow.StealRandomBuff(enemy, Owner.Creature, sourceCard: this);
await EnemyPowerFlow.InheritDebuffs(fromEnemy, toEnemy);
EnemyIntentInfo intent = EnemyIntentService.Get(enemy);
```

高级作者仍可下钻到低层（`PowerTransferCatalogue`、adapter、原版 STS2 command），但常见卡牌效果不应要求手动读取 `monster.NextMove.Intents`、维护 power 类名黑名单，或直接操作 `MonsterMoveStateMachine.StateLog`。

## 分层模型

| 层 | 目的 | 当前状态 |
| --- | --- | --- |
| `PowerTransfer` | 安全分类并转移敌方 power。 | `SafeClone` 已实现；adapter 可注册。 |
| `EnemyPowerFlow` | 高层动词：偷取、继承、转化为卡、移除。 | 计划中。 |
| `EnemyIntentService` | 不直接检查 `NextMove.Intents` 也能读取敌人意图。 | 计划中。 |
| `EnemyIntentOverride` | 受控替换敌人行动，例如睡眠、延迟、强制下一招。 | 延后；高风险。 |
| `EnemySnapshot` | powers、intent、标签、敌人角色、转移能力的一致快照。 | 计划中。 |

## 可执行阶段

### 阶段 1：Power Transfer 基础

状态：已实现。

- `PowerTransferCatalogue` 分类已知原版 power。
- 未知 power 默认 `Unsupported`。
- `PowerTransferService.TransferAsync` 执行 `SafeClone` power。
- `PowerTransferAdapterRegistry` 将 `NeedsAdapter` power 路由到消费模组提供的 adapter。

扩展前的退出条件：

- 增加一张游戏内 smoke card，偷取一个已知 `SafeClone` Buff。
- 验证源 power 移除、目标获得 power、战斗继续、存档稳定。

### 阶段 2：EnemyPowerFlow 便捷 API

目标：让 Achim 类卡牌变短、可读。

计划 API 形态：

```csharp
await EnemyPowerFlow
    .From(enemy)
    .WhereBuff()
    .PickRandom(Owner.RunState.Rng.CombatTargets)
    .TransferTo(Owner.Creature, sourceCard: this);
```

需要完成：

- 增加 buff/debuff/visible/amount-positive 等筛选器。
- 通过调用方传入的 STS2 RNG 做确定性随机选择。
- 为无候选、需要 adapter、不支持、已应用等情况返回结构化结果。
- 明确区分移除源 power 的 `Steal` 和不移除源 power 的 `Copy`。

### 阶段 3：内置 Adapter 样例

目标：证明 adapter 系统能解决真实 enemy-only power。

首批候选：

| Power | 原因 |
| --- | --- |
| `CurlUpPower` | 范围小；原版会写入 `LouseProgenitor.Curled`。 |
| `SkittishPower` | 常见敌方格挡触发器，带每回合状态。 |
| `StockPower` | 玩家侧计数替代实现比较直接。 |

只有在 KoraxLib 能安全注册自定义 power model，或能应用消费模组提供的替代 power 后再做。

### 阶段 4：Enemy Intent 只读 API

目标：替代直接读取 `monster.NextMove.Intents`。

计划 API 形态：

```csharp
EnemyIntentInfo info = EnemyIntentService.Get(enemy);
bool attacks = info.IntendsToAttack;
int damage = info.AttackDamage;
```

需要完成：

- 包装当前意图读取和攻击伤害计算。
- 当出招不可读时返回中性的 `Unknown`。
- 增加“如果敌人攻击但伤害为 0，则施加 debuff”类型效果的例子。

### 阶段 5：Enemy Intent Override

目标：替代直接 `SetMoveImmediate` 和 state-log 操作。

这是最晚做的阶段，因为它触碰最脆弱的 STS2 敌人内部结构。

计划 API 形态：

```csharp
await EnemyIntentOverride.SleepThisTurn(enemy);
await EnemyIntentOverride.DelayOneTurn(enemy);
```

需要完成：

- 安全保留 follow-up state。
- 避免破坏 state log。
- 至少用一个简单敌人、一个条件分支敌人、一个随机分支敌人做 smoke 测试。

## 当前边界

KoraxLib 当前支持安全 power 转移决策和 safe-clone 执行。完整的 `EnemyPowerFlow`、`EnemyIntentService`、move override API 尚未实现。

文档和示例必须明确区分当前能力与计划能力：高层计划 API 在源码存在前不能被写成已可用运行时接口。
