# 原版能力

::: warning 计划中的 API
Vanilla ability registry、preview、executor、primitive abilities 和 catalog 源码文件尚不存在。本页只记录设计方向，避免用户误认为这是已支持的运行时 API。
:::

KoraxLib 计划把经过审查的原版敌人行为暴露为中立能力定义。设计目标是在不把 `MonsterModel.PerformMove()` 当作玩家侧能力 API 调用的前提下，更安全地复用行为。

## 计划中的概念

| 概念 | 用途 |
| --- | --- |
| `VanillaAbilityDefinition` | 从已审查原版行为派生出的具名 ability entry。 |
| `VanillaAbilityRegistry` | 已知 definitions 的 catalog。 |
| `VanillaAbilityPreview` | 不修改战斗状态的 preview surface。 |
| `VanillaAbilityExecutor` | 带风险门控的执行入口。 |
| `VanillaAbilityRisk` | 风险等级：`Safe`、`ContextSensitive`、`Unsafe`。 |

## 风险模型

- `Safe`：不依赖原始 monster runtime state、encounter identity、private fields 或 visuals。
- `ContextSensitive`：需要调用方提供 targets、source、RNG 或 side policy。
- `Unsafe`：可以记录和文档化，但不应通过 normal executor 执行。

## 当前建议

在该模块实现前，消费模组应直接实现自己的 ability，或等待后续 catalog 工作完成。
