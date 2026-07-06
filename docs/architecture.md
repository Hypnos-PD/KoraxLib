# KoraxLib 架构文档

本文档描述 KoraxLib 的长期架构边界：模块如何分层、依赖如何流动、哪些概念属于公共 API，哪些属于内部实现。具体 Milestone 1 API 签名、行为要求和验收标准见 [spec.md](spec.md)。

本文档不是实现状态报告。

## 架构目标

KoraxLib 是一个 Slay the Spire 2 前置库模组。它要解决的核心问题是：让敌人相关系统和原版怪物能力更安全地被复用。

架构目标：

- 对消费方隐藏 Harmony patch、`ModelDb` 注入和 STS2 初始化顺序细节。
- 把“敌人注册/事件”和“原版怪物能力复用”拆成两个独立模块。
- 把原版怪物行动视为素材来源，而不是直接可复用 API。
- 通过风险分级表达能力复用的安全性。
- 保持前置库独立；RitsuLib、MinionLib 等只能作为兼容层，而不是核心依赖。

## 非目标

- 不替换 STS2 原版敌人 AI 系统。
- 不在第一阶段包装所有原版怪物。
- 不提供未经验证的大型 DSL。
- 不把 RitsuLib API 换名后重新导出。
- 不允许模组作者把 `MonsterModel.PerformMove()` 当作安全的玩家侧能力调用。

## 模块划分

计划中的源码树按稳定概念组织。

```text
src/
  Core/
    Diagnostics/
    Lifecycle/
    Patching/
    Registration/

  Content/
    Assets/
    Localization/
    ModelRegistry/

  Enemies/
    EnemyContext.cs
    EnemyEvents.cs
    EnemyPluginRegistry.cs
    EnemyRegistry.cs
    IEnemyPlugin.cs

  MonsterMoves/
    MoveStateIds.cs
    MoveStateMachineBuilders.cs

  VanillaAbilities/
    VanillaAbilityContext.cs
    VanillaAbilityDefinition.cs
    VanillaAbilityExecutor.cs
    VanillaAbilityPreview.cs
    VanillaAbilityRegistry.cs
    VanillaAbilityRisk.cs
    VanillaAbilityTags.cs

    Primitives/
    Catalog/

  Visuals/
    AnimationAliases.cs
    CreatureVisualFactories.cs

  Compatibility/
    MinionLib/
    RitsuLib/
```

没有实际代码或文档的模块不需要提前创建空目录。

## 模块职责

### Core

`Core` 只放 KoraxLib 自己的基础设施：日志、诊断、patch 安装、注册生命周期和通用初始化事件。

`Core` 不应该知道具体敌人、具体 vanilla ability、RitsuLib 或 MinionLib。

### Content

`Content` 负责和 STS2 模型/资源系统交互：`ModelDb` 合并、资源路径、本地化辅助等。

它可以服务 `Enemies` 和 `VanillaAbilities`，但不包含业务规则。

### Enemies

`Enemies` 把敌人视为战斗实体和遭遇内容。

职责：

- 注册 `MonsterModel` 类型。
- 注册 act-scoped 和 global `EncounterModel` 类型。
- 从 STS2 hooks 发布敌人生命周期事件。
- 把生命周期事件分发给敌人插件。
- 提供围绕 `Creature`、`MonsterModel`、`ICombatState` 的安全 context。

`Enemies` 不负责复用原版怪物行动。

### VanillaAbilities

`VanillaAbilities` 负责把选中的原版怪物行动拆成中立能力定义。

安全复用单位是 `VanillaAbilityDefinition`，不是 `MonsterModel`、`MoveState` 或 `MonsterModel.PerformMove()`。

职责：

- 管理 vanilla ability catalog。
- 提供 preview 和 execute 入口。
- 按风险等级限制执行。
- 提供 primitive ability，供 catalog entry 组合复用。

第一阶段 catalog 手写，不做自动转换。

### MonsterMoves

`MonsterMoves` 提供构造 move state machine 的小型辅助工具。它服务自定义敌人编写和 catalog 审查，但不拥有能力执行语义。

### Visuals

`Visuals` 管理 creature visuals、动画别名、动画 factory 等视觉层扩展。视觉层不能反向决定战斗规则。

### Compatibility

`Compatibility` 在外部库存在时提供适配。

兼容模块只能依赖 KoraxLib 公共模块，不能成为核心模块的前置条件。

## 依赖方向

依赖方向必须保持单向。

```text
Entry
  -> Core.Patching
  -> Core.Lifecycle
  -> Enemies
  -> VanillaAbilities

Enemies
  -> Core.Registration
  -> Core.Lifecycle
  -> Content.ModelRegistry

VanillaAbilities
  -> MonsterMoves
  -> Core.Diagnostics
  -> Enemies 仅限可选 context helper

Compatibility.*
  -> public KoraxLib modules
```

`Core` 不能依赖 `Enemies`、`VanillaAbilities`、`Visuals` 或 `Compatibility`。

## 初始化生命周期

`Entry.Initialize()` 保持薄入口。

```text
Entry.Initialize
  1. 创建 logger。
  2. 应用 KoraxLib Harmony patches。
  3. 打开注册服务。
  4. 注册内置 vanilla ability catalog。
  5. 注册当前 assembly 的 Godot scripts。
```

所有内容注册表共享同一个生命周期状态：`Open` 到 `Frozen`。

冻结发生在 `ModelDb.Init` 前或期间。冻结后禁止新增内容注册，但运行时事件订阅仍允许。

## 公共 API 边界

受支持的公共 API 放在：

```text
KoraxLib.Enemies
KoraxLib.MonsterMoves
KoraxLib.VanillaAbilities
```

内部实现放在：

```text
KoraxLib.Internal.*
```

消费方不应该依赖 `Internal` namespace。内部类型可以不保证兼容。

## 安全原则

Vanilla ability 的安全性按风险等级表达：

- `Safe`：不依赖原始怪物状态机、阵营、私有字段、遭遇身份或特定视觉节点。
- `ContextSensitive`：需要调用方提供正确 target、RNG、source 或 side policy。
- `Unsafe`：可以被 catalog 记录和文档化，但默认 executor 拒绝执行。

核心规则：执行 vanilla ability 时必须使用调用方提供的 `Creature User`、targets、source 和 RNG，不能复用原始 `MonsterModel` 的运行时状态。

## Patch 策略

KoraxLib 拥有自己的 Harmony instance。patch 类保持 internal，并按关注点分组。

```text
Internal.Patching.EnemyLifecyclePatches
Internal.Patching.ModelDbEnemyRegistrationPatches
Internal.Patching.RegistrationFreezePatches
```

patch 方法只做桥接：读取 STS2 参数、构造 KoraxLib context、调用 service。它们不包含 catalog 逻辑、plugin 策略或 ability 执行逻辑。

## 文档职责

- `architecture.md` 说明长期边界、依赖方向和设计原则。
- `spec.md` 说明当前里程碑的 API 合同、行为要求、测试要求和验收标准。

当二者冲突时，以 `spec.md` 对当前里程碑的实现要求为准；长期设计调整后应同步更新本文件。
