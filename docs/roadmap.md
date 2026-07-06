# KoraxLib Roadmap

本文档定义 KoraxLib 的渐进实现路线。它不替代 [architecture.md](architecture.md) 或 [spec.md](spec.md)：架构边界看 `architecture.md`，Milestone 1 的具体 API 合同看 `spec.md`。

Roadmap 的作用是限制阶段范围，避免在基础能力尚未验证前扩张到大型 DSL、全量 catalog 或兼容层。

## M0: Scaffold

目标：确认 KoraxLib 作为 STS2 前置库模组可以独立构建、部署，并具备可复现开发环境。

已完成：

- `KoraxLib.csproj`、`KoraxLib.sln`、`mod_manifest.json` 已建立。
- `Entry.Initialize()` 已提供最小初始化入口。
- `local.props.template` 已记录本地 STS2 路径配置方式。
- `flake.nix` 和 `flake.lock` 已提供 Nix dev shell。
- `global.json` 已固定 .NET SDK 版本。
- 本地 `dotnet build` 已通过，并能把 dll 和 manifest 部署到 STS2 `mods/KoraxLib`。
- STS2 模组列表中已能看到 KoraxLib。
- 运行时日志已确认 KoraxLib assembly 被加载，`Entry.Initialize()` 被调用，并输出 `KoraxLib v0.1.0 initialized.`。

退出条件：

- 项目可在本地开发环境稳定 build。
- 文档说明清楚如何配置 STS2 路径。
- 后续开发不依赖 RitsuLib 内部实现。


## M1: Enemy Lifecycle + Vanilla Ability Skeleton

目标：实现 `spec.md` 当前范围，让 KoraxLib 具备第一批可用公共 API。

范围：

- 内容注册生命周期：`Open` 到 `Frozen`。
- `MonsterModel` 和 `EncounterModel` 注册入口。
- enemy lifecycle events。
- `IEnemyPlugin` 注册与分发。
- vanilla ability registry、preview、executor skeleton。
- safe/context-sensitive/unsafe 风险门控。
- 第一批手写 vanilla ability catalog。

当前进度：

- 已完成 `Core/Registration` 生命周期骨架。
- 已完成 `EnemyRegistry` 纯注册表层：类型验证、重复注册 no-op、freeze guard、只读快照。
- 已完成 registered monster 到 STS2 `ModelDb` 的接入：`ModelDb.Init` 冻结/注入，`ModelDb.Monsters` getter 合并枚举。
- 尚未完成 registered encounter 到 act encounter lists 的实际合并 patch。

退出条件：

- `EnemyRegistry` 能把 monster 和 encounter 合并进 STS2 model lists。
- enemy lifecycle hooks 能发布到 `EnemyEvents`。
- `IEnemyPlugin` 只收到匹配的 enemy context。
- safe vanilla ability 可以 preview 和 execute。
- unsafe vanilla ability 默认不能通过 normal executor 执行。
- 不调用 `MonsterModel.PerformMove()` 作为 ability 复用方式。
- 至少一个 ability 通过 driver 或游戏内 smoke test 验证。

暂不做：

- attribute auto-registration。
- 自动转换原版 monster move state machine。
- 全量 vanilla ability catalog。
- RitsuLib 或 MinionLib 兼容层。
- 自定义 UI 或调试面板。

## M2: First Real Enemy Integration

目标：用一个最小真实敌人验证 enemy registration、encounter registration、lifecycle events 和 visuals 边界。

范围：

- 添加一个 KoraxLib 示例或测试敌人。
- 添加一个最小 encounter。
- 验证 spawn、turn start、death events。
- 验证自定义敌人能独立使用 KoraxLib 注册入口。
- 明确 visuals 只作为表现层，不参与战斗规则。

退出条件：

- 游戏内能遇到该测试敌人。
- 该敌人的生命周期事件可被插件观察到。
- encounter 注册路径不依赖外部库。
- 文档记录实际 patch target 和任何 STS2 行为限制。

暂不做：

- 多 act 内容包。
- 复杂动画系统。
- 面向内容作者的大型敌人 DSL。

## M3: Vanilla Ability Catalog Expansion

目标：在 M1 executor 可靠后，逐步扩展 vanilla ability catalog。

范围：

- 按怪物或能力类型分批添加 catalog entries。
- 每个 entry 记录原始 monster type、move ID、risk、tags、preview 和 execute。
- 优先添加 `Safe` 和容易验证的 `ContextSensitive` ability。
- 对无法安全迁移的行为只登记为 `Unsafe` 或暂不登记。

退出条件：

- 每个新增 ability 都有 source 注释和风险说明。
- preview 不改变 combat state。
- executor 使用调用方 context，不读取原始 monster runtime state。
- catalog 扩展有测试或 smoke test 覆盖。

暂不做：

- 全自动 catalog 生成。
- 把 unsafe ability 通过 normal executor 暴露给消费方。
- 为未验证 ability 提供兼容 fallback。

## M4: Compatibility Adapters

目标：在核心 API 稳定后，再考虑外部生态兼容。

候选方向：

- RitsuLib compatibility adapter。
- MinionLib compatibility adapter。
- 示例模组迁移指南。

进入条件：

- M1 和 M2 的核心 enemy API 已稳定。
- 至少一个真实消费场景证明需要兼容层。
- 兼容层不会让核心模块依赖外部库。

暂不做：

- 在核心 API 未稳定前承诺兼容语义。
- 把外部库的注册模型直接变成 KoraxLib 的内部模型。

## Explicitly Deferred

以下内容明确延期，除非有新的需求或 blocker 证明必须提前做：

- 大型敌人 DSL。
- 自动扫描 assembly 并注册所有内容。
- 全量原版怪物能力 catalog。
- 可视化 catalog viewer。
- Steam/workshop 打包工具。
- 运行时配置 UI。
- 对 RitsuLib 内部类型的硬依赖。

## Documentation Rules

- 新增长期架构原则时，更新 `architecture.md`。
- 新增或变更 M1 API 合同时，更新 `spec.md`。
- 改变阶段顺序、退出条件或延期项时，更新本文档。
- 研究笔记不要塞进 roadmap；如果需要，另建 `dev-notes.md`。
