# API 概览

KoraxLib 当前公共 API 主要位于 `KoraxLib.Enemies` 和 `KoraxLib.Core.Registration`。

## 稳定公共命名空间

```text
KoraxLib.Enemies
KoraxLib.Core.Registration
```

消费模组应只依赖这些公共命名空间。

## Internal 命名空间

```text
KoraxLib.Internal.*
```

::: warning Internal API
`KoraxLib.Internal.*` 下的类型用于连接 STS2、Harmony、Godot 和 smoke-test 基础设施。消费模组不应依赖这些类型；它们不提供兼容性保证。
:::

## 当前模块

| 领域 | 状态 | 入口 |
| --- | --- | --- |
| 敌人注册 | 已实现 | `EnemyRegistry` |
| 敌人生命周期事件 | 已实现 | `EnemyEvents`、`IEnemyPlugin`、`EnemyPluginRegistry` |
| 注册生命周期 | 已实现 | `KoraxRegistrationState` |
| 原版能力 | 计划中 | 源码尚未实现 |

## 验证状态

当前 enemy 路径已通过游戏内 smoke 验证：

- 初始 encounter 中 `Nibbit` 的 `EnemySpawned`。
- 普通死亡路径的 `EnemyDying` 和 `EnemyDied`。
- `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` 胜利结算和存档序列化。
- 存档时未再出现 `Unknown ModelId entry 'ENCOUNTER.KORAX_SMOKE_ENCOUNTER'`。
