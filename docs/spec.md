# KoraxLib Milestone 1 SPEC

本文档定义 Milestone 1 的实现合同：API 形状、行为要求、测试要求和验收标准。架构背景、模块分层和长期设计原则见 [architecture.md](architecture.md)。

## 状态

草案。本文档中的 API 只有在对应代码和测试存在后才算实现。

## 范围

Milestone 1 提供：

- 敌人生命周期事件：spawned、dying、died、side-turn start timing。
- 敌人插件注册与分发。
- `MonsterModel` 和 `EncounterModel` 类型注册入口。
- vanilla ability registry、executor、preview API。
- 小型手写 vanilla ability catalog。
- 简单伤害、多段伤害、格挡、治疗、施加 power 的 primitive ability。

Milestone 1 不提供：

- 全量原版怪物 catalog。
- 自动转换原版怪物 move state machine。
- attribute auto-registration。
- 用于编写怪物的大型公共 DSL。
- 自定义 encounter scene 生成。
- 对 RitsuLib 或 MinionLib 的硬依赖。

## 术语

**Enemy** 指运行时 `Creature`，它是 monster，并且在当前玩家语境中位于对立阵营。没有玩家语境的 patch 点使用 `Creature.IsMonster && Creature.IsEnemy` 作为主要过滤条件。

**Monster model** 指注册到 STS2 `ModelDb` 的 `MonsterModel` 子类型。

**Encounter model** 指 act encounter pool 使用的 `EncounterModel` 子类型。

**Vanilla ability** 指从原版怪物 move 或行为派生出的中立 KoraxLib 能力定义。

**Primitive ability** 指一个或多个 vanilla ability definition 复用的基础执行积木。

## 注册生命周期

```csharp
public enum KoraxRegistrationState
{
    Open,
    Frozen,
}
```

要求：

- 注册表在 `Entry.Initialize()` 期间以 `Open` 启动。
- 注册表在 `ModelDb.Init` 完成模型缓存前冻结。
- 冻结后进行内容注册会抛出 `InvalidOperationException`。
- 同一类型重复注册是 no-op，并以 debug 级别记录日志。
- public ID 冲突必须在 freeze 前或 freeze 期间失败。
- 运行时事件订阅不是内容注册，freeze 后仍然允许。

实现状态：

- 已实现 `KoraxRegistrationState` 和 internal `RegistrationLifecycle`。
- 已通过 `ModelDb.Init` prefix 调用 `RegistrationLifecycle.Freeze()`。

## Enemy Registry

Namespace：`KoraxLib.Enemies`

```csharp
public static class EnemyRegistry
{
    public static KoraxRegistrationState State { get; }
    public static IReadOnlyCollection<Type> RegisteredMonsters { get; }
    public static IReadOnlyCollection<Type> RegisteredGlobalEncounters { get; }
    public static IReadOnlyDictionary<Type, IReadOnlyCollection<Type>> RegisteredActEncounters { get; }

    public static void RegisterMonster<TMonster>()
        where TMonster : MonsterModel;

    public static void RegisterMonster(Type monsterType);

    public static void RegisterActEncounter<TAct, TEncounter>()
        where TAct : ActModel
        where TEncounter : EncounterModel;

    public static void RegisterActEncounter(Type actType, Type encounterType);

    public static void RegisterGlobalEncounter<TEncounter>()
        where TEncounter : EncounterModel;

    public static void RegisterGlobalEncounter(Type encounterType);
}
```

行为要求：

- 已注册集合通过防御性快照暴露，消费端不能修改内部 registry。
- `RegisterMonster` 验证类型是 concrete closed `MonsterModel` 子类型。
- `RegisterActEncounter` 验证类型是 concrete closed `ActModel` 和 `EncounterModel` 子类型。
- `RegisterGlobalEncounter` 验证类型是 concrete closed `EncounterModel` 子类型。
- 同一类型重复注册是 no-op，并以 debug 级别记录日志。
- 已注册 monster 合并进 `ModelDb.Monsters`。
- 已注册 act encounter 合并进匹配 act 的 generated encounter list。
- 已注册 global encounter 合并进所有支持的 act encounter lists。

实现状态：

- 已实现纯 registry 层：类型验证、重复注册 no-op、freeze guard、只读快照。
- 已实现 `ModelDb.Init` postfix，通过 `ModelDb.Inject(type)` 确保注册 monster 进入 STS2 canonical model database。
- 已实现 registered encounter 的 `ModelDb` 接入：动态程序集类型在 `ModelDb.Init` prefix 提前注入，静态 DLL 类型交给原版扫描，`ModelDb.Init` postfix 再兜底注入遗漏类型。
- 已实现 `ModelDb.Monsters` getter postfix，把注册 monster 追加进 monster 枚举并按 model ID 去重。
- 已实现 act encounter list 动态合并 patch：在 `ModelDb.Init` prefix 阶段扫描所有具体 `ActModel` 子类型，为每个子类型的 `GenerateAllEncounters()` 实现动态安装 Harmony postfix。

Patch 来源：

- `ModelDb.Init` prefix 冻结 KoraxLib 内容注册。
- `ModelDb.Init` prefix 注入动态程序集中的已注册 encounter 类型。
- `ModelDb.Init` prefix 动态发现具体 `ActModel` 子类型并安装 encounter 合并 postfix。
- `ModelDb.Init` postfix 注入已注册 monster 类型。
- `ModelDb.Init` postfix 兜底注入所有已注册 encounter 类型，避免原版扫描遗漏。
- `ModelDb.Monsters` getter postfix 合并已注册 monster。
- `ActModel.GenerateAllEncounters()` 及各具体 act override 的动态 postfix：合并 act-scoped encounter 和 global encounter。

Encounter 合并规则：
- 保留原版 encounter 的原始顺序。
- 追加 act-scoped encounter（来自 `EnemyRegistry.RegisteredActEncounters[actType]`）。
- 追加 global encounter（来自 `EnemyRegistry.RegisteredGlobalEncounters`）。
- 按 `EncounterModel.Id` 去重，已存在的 encounter 不重复添加。

Patch 实现位置：
- `src/Internal/Patching/ModelDbEnemyRegistrationPatches.cs`：monster 冻结/注入/枚举合并。
- `src/Internal/Patching/ModelDbEncounterRegistrationPatches.cs`：encounter 注入、动态 ActModel 子类型扫描、`GenerateAllEncounters()` postfix 安装和 encounter 合并逻辑。

## Enemy Context

```csharp
public sealed record EnemyContext(
    ICombatState CombatState,
    Creature Creature,
    MonsterModel Monster
);
```

要求：

- `Creature.Monster` 必须非 null。
- 可用时，`Creature.CombatState` 必须与 `CombatState` 匹配。
- 非 monster creature 构造 context 时失败。
- 只有要求活体敌人的事件才拒绝 dead creature。死亡事件可以携带 dead creature。

## Enemy Events

```csharp
public static class EnemyEvents
{
    public static event Func<EnemyContext, Task>? EnemySpawned;
    public static event Func<EnemyContext, Task>? EnemyTurnStarting;
    public static event Func<EnemyContext, Task>? EnemyTurnStarted;
    public static event Func<EnemyDyingContext, Task>? EnemyDying;
    public static event Func<EnemyDiedContext, Task>? EnemyDied;
}

public sealed record EnemyDyingContext(
    IRunState RunState,
    ICombatState CombatState,
    Creature Creature,
    MonsterModel Monster
);

public sealed record EnemyDiedContext(
    IRunState RunState,
    ICombatState CombatState,
    Creature Creature,
    MonsterModel Monster,
    bool WasRemovalPrevented,
    float DeathAnimationDurationSeconds
);
```

Patch 来源：

- `Hook.AfterCreatureAddedToCombat` 发布 `EnemySpawned`。
- `Hook.BeforeSideTurnStart` 对 enemy participants 发布 `EnemyTurnStarting`。
- `Hook.AfterSideTurnStart` 对 enemy participants 发布 `EnemyTurnStarted`。
- `Hook.BeforeDeath` 发布 `EnemyDying`。
- `Hook.AfterDeath` 发布 `EnemyDied`。

分发要求：

- handler 按注册顺序执行。
- source hook 返回 `Task` 时，async handler 通过 patched task bridge 被 await。
- 单个 handler 失败会记录日志，并且不阻止后续 handler，除非未来 SPEC 把该事件标记为 critical。
- 事件不得为 player creature 发布。
- 事件不得为 null 或 detached combat state 发布。

## Enemy Plugins

```csharp
public interface IEnemyPlugin
{
    bool AppliesTo(EnemyContext context);

    Task OnSpawned(EnemyContext context) => Task.CompletedTask;
    Task OnTurnStarting(EnemyContext context) => Task.CompletedTask;
    Task OnTurnStarted(EnemyContext context) => Task.CompletedTask;
    Task OnDying(EnemyDyingContext context) => Task.CompletedTask;
    Task OnDied(EnemyDiedContext context) => Task.CompletedTask;
}

public static class EnemyPluginRegistry
{
    public static void Register(IEnemyPlugin plugin);
    public static bool Unregister(IEnemyPlugin plugin);
}
```

要求：

- plugin registration 在 content freeze 后仍然允许。
- 重复注册同一 plugin instance 会被忽略。
- `AppliesTo` 每次事件都会重新评估。
- `AppliesTo` 返回 false 时，plugin 不得收到事件。
- plugin 分发顺序是注册顺序。

## Vanilla Ability Registry

Namespace：`KoraxLib.VanillaAbilities`

```csharp
public static class VanillaAbilityRegistry
{
    public static void Register(VanillaAbilityDefinition definition);
    public static bool TryGet(string id, out VanillaAbilityDefinition definition);
    public static IReadOnlyCollection<VanillaAbilityDefinition> All { get; }
}

public sealed record VanillaAbilityDefinition(
    string Id,
    Type OriginalMonsterType,
    string OriginalMoveId,
    IReadOnlySet<string> Tags,
    VanillaAbilityRisk Risk,
    Func<VanillaAbilityContext, IReadOnlyList<AbstractIntent>> Preview,
    Func<VanillaAbilityContext, Task> Execute
);
```

要求：

- ID 使用小写和 slash：`vanilla:axebot/hammer_uppercut`。
- duplicate ID 抛异常，除非现有 definition 与新 definition 是同一引用。
- registry 与其他 content registries 一起 freeze。
- `OriginalMonsterType` 必须继承 `MonsterModel`。
- `Tags` 不能为空。

## Vanilla Ability Context

```csharp
public sealed record VanillaAbilityContext(
    ICombatState CombatState,
    Creature User,
    IReadOnlyList<Creature> Targets,
    AbstractModel? Source,
    Rng Rng,
    VanillaAbilitySidePolicy SidePolicy
);

public enum VanillaAbilitySidePolicy
{
    PreserveTargets,
    UserOpponentsOnly,
    UserAlliesOnly,
}
```

要求：

- `User` 必须存活，除非 ability 明确支持 dead user。
- `Targets` 必须在执行前解析完成。
- executor 不能从原始 monster move 推断 target。
- `Rng` 由调用方提供。executor 不使用来源 monster 上的 `MonsterModel.Rng`。

## Vanilla Ability Risk

```csharp
public enum VanillaAbilityRisk
{
    Safe,
    ContextSensitive,
    Unsafe,
}
```

执行规则：

- `Safe` ability 通过 normal executor 执行。
- `ContextSensitive` ability 只有 context validation 通过时才通过 normal executor 执行。
- `Unsafe` ability 可发现，但 normal executor 拒绝执行。

## Vanilla Ability Executor

```csharp
public static class VanillaAbilityExecutor
{
    public static Task Execute(string id, VanillaAbilityContext context);
    public static Task Execute(VanillaAbilityDefinition definition, VanillaAbilityContext context);
}
```

要求：

- 未知 ID 抛 `KeyNotFoundException`。
- unsafe definition 抛 `InvalidOperationException`。
- 执行前运行 context validation。
- 尽量使用 STS2 command API 执行，以保持 hooks、统计、伤害修正和 powers 正常工作。
- 执行绝不调用 `MonsterModel.PerformMove()`。

## Vanilla Ability Preview

```csharp
public static class VanillaAbilityPreview
{
    public static IReadOnlyList<AbstractIntent> GetIntents(string id, VanillaAbilityContext context);
    public static IReadOnlyList<AbstractIntent> GetIntents(VanillaAbilityDefinition definition, VanillaAbilityContext context);
}
```

要求：

- preview 不得修改 combat state。
- preview 不得滚动持久 RNG state，除非未来 SPEC 明确允许 preview RNG。
- unsupported ability 只有在 definition 明确说明没有 intent 时，才可以返回空列表。

## Primitive Abilities

Milestone 1 primitives：

```text
AttackAbility
MultiAttackAbility
ApplyPowerAbility
GainBlockAbility
HealAbility
```

要求：

- primitive constructor 验证静态配置。
- primitive execution 验证 context。
- damage primitive 使用 context `User` 作为 dealer，并在 STS2 API 支持时使用 `Source` 作为 source model。
- power primitive 默认使用 context `User` 作为 applier，除非另有配置。
- primitive 执行时不得检查 `OriginalMonsterType`。

## 初始 Vanilla Catalog

Milestone 1 的最小 catalog：

```text
vanilla:axebot/boot_up
vanilla:axebot/hammer_uppercut
vanilla:axebot/one_two
vanilla:mawler/claw
vanilla:mawler/rip_and_tear
vanilla:mawler/roar
vanilla:chomper/clamp
vanilla:chomper/screech
```

每个 catalog entry 必须包含：

- 原始 monster type。
- 原始 move ID。
- tags。
- risk。
- preview function。
- execute function。
- 注释：引用 vanilla source class，并说明 adapter 为什么 safe 或 context-sensitive。

## Tags

标准 tags：

```text
attack
multi_attack
block
heal
buff
debuff
status_card
random_target
summon
stateful
encounter_bound
safe_for_player
safe_for_enemy
safe_for_pet
preview_only
```

tags 使用 string，方便未来扩展而不频繁改 enum。内置 tags 应该提供常量。

## Diagnostics

KoraxLib 必须记录：

- patch application failures。
- freeze 后的注册尝试。
- duplicate IDs。
- unsafe ability execution attempts。
- plugin exceptions。
- freeze validation 失败的 catalog entries。

Milestone 1 不要求用户可见的 diagnostics UI。

## 测试要求

实现时的最低验证：

- 单元测试 registry state transitions。
- 单元测试 duplicate registration behavior。
- 单元测试 ability ID validation。
- 单元测试 safe/context-sensitive/unsafe execution gates。
- 使用本地 STS2 assemblies build KoraxLib。
- 可选运行 `KORAXLIB_ENABLE_SMOKE_CONTENT=1`，把 internal `KoraxSmokeEncounter` 注册进 `Overgrowth`，用于验证 registered encounter 是否进入 act encounter list。
- 已用 `scripts/run-smoke-linux.sh` 在 Linux/Steam runtime 下启动到主菜单，日志确认 smoke encounter 注册已启用。脚本默认读取项目根目录 `.env`，可从 `.env.example-linux` 复制本地路径配置。
- 声明 runtime behavior 完成前，至少通过 driver 或游戏内场景 smoke-test 一个 ability。

## 验收标准

Milestone 1 完成条件：

- enemy lifecycle events 从文档列出的 hook sources 发布。
- `IEnemyPlugin` 只收到匹配的 enemy contexts。
- monster 和 encounter registrations 合并进对应 STS2 model lists。
- vanilla ability registry 包含初始 catalog。
- safe catalog entries 可以 preview 和 execute，且不调用 `MonsterModel.PerformMove()`。
- unsafe catalog entries 不能通过 normal executor 执行。
- 文档示例能够编译，或明确标记为 pseudo-code。

## 未决问题

- enemy turn events 应包含所有 enemy participants，还是只包含 living、hittable enemies？
- plugin exceptions 是否默认永远吞掉，还是 debug 设置下可以 fatal？
- 哪些 STS2 command API 最能保留玩家使用怪物能力时的 source attribution？
- `VanillaAbilityContext` 应继续使用 `IReadOnlyList<Creature>`，还是换成更丰富的 target selection object？
- Milestone 1 是否需要 unsafe execution path，还是只需要 discovery 和 rejection？

## 已决问题

- act encounter patch target：确认为 `ActModel.GenerateAllEncounters()` 及各具体 act override。在 `ModelDb.Init` prefix 中动态扫描 `ActModel` 子类型并安装 postfix；动态程序集中的 registered encounter 会在 prefix 中提前注入，静态 DLL 类型由原版扫描/postfix 兜底处理（实现于 `ModelDbEncounterRegistrationPatches.PrepareRegisteredEncountersBeforeModelDbInit`）。
