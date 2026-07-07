# 敌人与遭遇

使用 KoraxLib 处理敌人时有三层互补机制：

- **行为** — 定义你的敌人在战斗中做什么（出招、意图、自定义状态）。使用 `KoraxEnemy`。
- **注册** — 告诉 STS2 关于你的敌人和它的遭遇战。使用 `EnemyRegistry`。
- **交互** — 安全检查或操作原版敌人的 power。当前使用 `PowerTransfer`；计划中的高层 API 见 [敌人交互设计](../development/enemy-interaction-design)。

创建新敌人时使用 `KoraxEnemy`。卡牌或遗物需要和已有原版敌人交互时，使用 `PowerTransfer`。

## 行为：使用 KoraxEnemy 定义敌人

::: warning 新 API
`KoraxEnemy` 是创建敌人的推荐方式。原始的 `MonsterModel` 方式仍然可用，但需要更多样板代码。
:::

`KoraxEnemy` 继承自 STS2 的 `MonsterModel`，提供了流式出招图构建器。继承它并实现 `ConfigureMoves`：

```csharp
using KoraxLib.Enemies.Behaviour;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

public sealed class MyNibbit : KoraxEnemy
{
    // ── 生命值（继承自 MonsterModel） ─────────────────────

    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

    public override int MaxInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 48, 46);

    // ── 音效（继承自 MonsterModel） ────────────────────────

    public override string DeathSfx =>
        "event:/sfx/enemy/enemy_attacks/nibbit/nibbit_die";

    // ── 出招 ──────────────────────────────────────────────

    protected override void ConfigureMoves(MoveGraph moves)
    {
        // 进阶等级缩放后的伤害 / 格挡值
        var buttDmg  = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);
        var sliceDmg = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
        var sliceBlk = AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 6, 5);
        var hissStr  = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

        // 条件初始状态（类似原版 Nibbit 的前排/后排逻辑）
        moves.InitialState(when => when
            .When(creature => ((MyNibbit)creature.Monster).HasFlag("isFront"), "slice")
            .Else("hiss"));

        // 出招通过 .Then() 形成确定性循环
        moves.Move("butt", move => move
            .Intent(new SingleAttackIntent(buttDmg))
            .OnPerform(async targets =>
            {
                await DamageCmd.Attack(buttDmg)
                    .FromMonster(this)
                    .WithAttackerAnim("Attack", 0.15f)
                    .Execute(null);
            })
            .Then("slice"));

        moves.Move("slice", move => move
            .Intent(new SingleAttackIntent(sliceDmg))
            .Intent(new DefendIntent())
            .OnPerform(async targets =>
            {
                await DamageCmd.Attack(sliceDmg)
                    .FromMonster(this)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(null);
                await CreatureCmd.GainBlock(Creature, sliceBlk, ValueProp.Move, null);
            })
            .Then("hiss"));

        moves.Move("hiss", move => move
            .Intent(new BuffIntent())
            .OnPerform(async targets =>
            {
                await PowerCmd.Apply<Strength>(Creature, hissStr).Execute(null);
            })
            .Then("butt"));
    }
}
```

### 自定义可变状态

使用 `SetFlag` / `GetFlag<T>` / `HasFlag` 实现每个克隆体的运行时状态，例如前排/后排位置：

```csharp
// 在你的 EncounterModel.GenerateMonsters() 中：
var frontNibbit = ModelDb.Monster<MyNibbit>().ToMutable() as MyNibbit;
KoraxEnemy.From(frontNibbit).SetFlag("isFront", true);
```

状态标志的作用域限定在可变克隆体上——每个战斗实例都通过 `AfterCloned` 获得自己独立的字典。

### 出招图功能

| 功能 | API | 原版等价物 |
| --- | --- | --- |
| 确定性循环 | `.Move(id, ...).Then(next)` | `MoveState.FollowUpState` |
| 条件初始状态 | `.InitialState(when => when.When(...))` | `ConditionalBranchState` |
| 加权随机分支 | `.RandomBranch(id, ...)` | `RandomBranchState` |
| 多回合锁定 | `.RequireCompletion()` | `MoveState.MustPerformOnceBeforeTransitioning` |
| 多个意图 | `.Intent(...).Intent(...)` | `MoveState` 构造函数参数 |
| 冷却控制 | `RandomBranchConfigurator.Branch(..., cooldown: 2)` | `RandomBranchState.AddBranch(cooldown)` |

## 注册：告诉 STS2 你的内容

消费模组在初始化期间通过 `EnemyRegistry` 声明敌人相关内容。KoraxLib 会在注册窗口开放时记录这些声明，并在安全的初始化阶段把它们应用到 STS2 model 和 encounter 结构中。

### 注册 Monster

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterMonster<MyNibbit>();
```

类型必须是 concrete closed `MonsterModel` 子类型。

### 注册 Act Encounter

```csharp
using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Models;

EnemyRegistry.RegisterActEncounter<Overgrowth, MyEncounterModel>();
```

Act-scoped encounters 会追加到目标 act 的 vanilla encounters 后面。KoraxLib 按 `EncounterModel.Id` 去重。

### 注册 Global Encounter

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterGlobalEncounter<MyEncounterModel>();
```

Global encounters 会追加到所有支持 act 的 encounter list 中，顺序在 act-scoped entries 之后。

### 注册生命周期

`EnemyRegistry.State` 暴露当前注册状态：

```csharp
public enum KoraxRegistrationState
{
    Open,
    Frozen,
}
```

内容注册必须发生在 `Open` 状态。KoraxLib 冻结注册后，再注册内容会抛出 `InvalidOperationException`。

运行时事件订阅不属于内容注册；freeze 后仍然允许。

### 重复注册

重复注册同一类型是 no-op。KoraxLib 会用 debug 日志记录重复声明，并保留第一次声明。

### 存档序列化

KoraxLib patch 了 `ModelIdSerializationCache.Init`，把 registered monster / encounter IDs 补入 STS2 的存档和网络序列化缓存。Smoke encounter 已验证 `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` 可以保存，不会被写成 `NONE`。
