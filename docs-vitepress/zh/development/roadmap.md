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

## 下一块实现

下一块大的 M1 工作是 vanilla ability registry、preview、executor skeleton、primitive abilities 和第一批手写 catalog entries。

## 明确延期

- 大型敌人 DSL。
- 自动扫描 assembly。
- 全量 vanilla monster ability catalog。
- 可视化 catalog viewer。
- Steam 或 Workshop 打包工具。
- 运行时配置 UI。
- 对 RitsuLib internal 类型的硬依赖。
