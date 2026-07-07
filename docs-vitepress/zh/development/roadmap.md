# 开发路线图

本页概述仓库中的 `docs/roadmap.md`。

## 当前状态

M1 enemy registration 和 lifecycle 基础设施已部分完成：

- 注册生命周期骨架已存在。
- `EnemyRegistry` 已提供 monster 和 encounter 声明 API。
- Registered monsters 已合并进 STS2 `ModelDb`。
- Registered encounters 已合并进 act encounter lists。
- Registered monster / encounter IDs 已补入 `ModelIdSerializationCache`。
- Enemy lifecycle events 和 plugin dispatch 已实现。
- 内部 smoke encounter 已验证初始 spawn、死亡生命周期、胜利结算和存档序列化。
- `KoraxEnemy` 已提供用于自定义敌人行为的流式出招图基础类。
- `PowerTransfer` 已提供原版敌人 power 分类、safe-clone 转移和消费模组注册 adapter。

## 下一块实现

下一块实现是 [敌人交互设计](./enemy-interaction-design) 中描述的高层敌人交互层：

1. 增加一张 smoke/demo card，在游戏内验证 `PowerTransferService.TransferAsync`。
2. 构建 `EnemyPowerFlow` 便捷 API，用于偷取、复制、继承、移除敌方 power。
3. 在尝试任何 move override API 前，先增加只读 `EnemyIntentService`。

## 明确延期

- 大型敌人 DSL。
- 自动扫描 assembly。
- 全量 vanilla monster ability catalog。
- 会操作 `MonsterMoveStateMachine` 的 enemy move override API。
- 可视化 catalog viewer。
- Steam 或 Workshop 打包工具。
- 运行时配置 UI。
- 对 RitsuLib internal 类型的硬依赖。
