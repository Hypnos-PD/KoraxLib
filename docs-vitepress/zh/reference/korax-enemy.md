# KoraxEnemy

`KoraxEnemy` 是创建自定义敌人的推荐基础类。它继承自 STS2 的 `MonsterModel`，提供流式出招图构建器和可变状态辅助方法。

```csharp
namespace KoraxLib.Enemies.Behaviour;

public abstract class KoraxEnemy : MonsterModel
{
    protected abstract void ConfigureMoves(MoveGraph moves);

    protected void SetFlag(string key, object? value);
    protected T? GetFlag<T>(string key, T? defaultValue = default);
    protected bool HasFlag(string key);

    public static KoraxEnemy From(MonsterModel monster);
}
```

## ConfigureMoves

重写 `ConfigureMoves` 使用 `MoveGraph` 定义敌人的出招状态机：

```csharp
protected override void ConfigureMoves(MoveGraph moves)
{
    moves.InitialState(when => when
        .When(creature => SomeCondition(creature), "moveA")
        .Else("moveB"));

    moves.Move("moveA", move => move
        .Intent(new SingleAttackIntent(6))
        .OnPerform(async targets => {
            await DamageCmd.Attack(6).FromMonster(this).Execute(null);
        })
        .Then("moveB"));

    moves.Move("moveB", move => move
        .Intent(new DefendIntent())
        .OnPerform(async targets => {
            await CreatureCmd.GainBlock(Creature, 5).Execute(null);
        })
        .Then("moveA"));
}
```

该方法在每个规范实例上调用一次。每个战斗可变克隆都会从同一份定义构建新的 `MonsterMoveStateMachine`。

## SetFlag / GetFlag / HasFlag

每个克隆体上存储的自定义可变状态。用于每个实例的行为标志（前排/后排槽位、孤立检测、阶段跟踪）。

```csharp
// 在 EncounterModel.GenerateMonsters() 中：
var enemy = KoraxEnemy.From(myMutableClone);
enemy.SetFlag("isFront", true);

// 在 InitialState 条件中：
moves.InitialState(when => when
    .When(creature => ((MyEnemy)creature.Monster).HasFlag("isFront"), "attack")
    .Else("defend"));
```

状态标志在 `AfterCloned()` 中重置为空字典——每个可变克隆体获得自己独立的状态。

## KoraxEnemy.From

将 `MonsterModel` 安全转换为 `KoraxEnemy`。如果模型不是 `KoraxEnemy` 子类则抛出 `InvalidOperationException`。

## 生命值与其他属性

`KoraxEnemy` **不**抽象 HP、SFX、视觉资源或动画属性。直接在子类上重写它们，与编写原始 `MonsterModel` 完全相同：

```csharp
public override int MinInitialHp =>
    AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

public override string DeathSfx =>
    "event:/sfx/enemy/enemy_attacks/my_enemy/my_enemy_die";
```

## GenerateMoveStateMachine

由 `KoraxEnemy` 自动实现。调用 `MoveGraph.Build(this)` 从 `ConfigureMoves` 中定义的图生成 `MonsterMoveStateMachine`。标记为 `sealed override`——子类无法重写。

## AfterCloned

被重写以将 `_runtimeState` 重置为每个可变克隆体的新字典。需要额外克隆清理的子类应调用 `base.AfterCloned()`。

## 参见

- [EnemyRegistry](./enemy-registry) — 类型注册与遭遇战声明
- [EnemyEvents](./enemy-events) — 生命周期钩子事件
- [敌人指南](../guide/enemy-guide) — 包含解释的完整示例
