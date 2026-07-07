# 架构说明

本页概述 `docs/architecture.md`。

## 目标

KoraxLib 是一个 Slay the Spire 2 前置库模组，面向敌人相关系统和更安全的原版敌人行为复用。

架构目标是对消费模组隐藏 Harmony patches、`ModelDb` 注入和 STS2 初始化顺序细节。

## 模块边界

- `Core`：注册生命周期、诊断、patching 基础设施。
- `Enemies`：monster / encounter 注册、lifecycle events、enemy plugin dispatch、enemy contexts。
- `VanillaAbilities`：计划中的原版行为复用模块。
- `MonsterMoves`：计划中的 move-state authoring 和 review 辅助。
- `Visuals`：计划中的表现层辅助；visuals 不拥有战斗规则。
- `Compatibility`：未来的外部库适配层。

## 依赖方向

Core 模块不应依赖 enemy、ability、visual 或 compatibility 模块。Compatibility 模块可以依赖 KoraxLib 公共模块，但不能成为核心前置条件。

## 公共 API 边界

受支持的公共 API 应位于：

```text
KoraxLib.Enemies
KoraxLib.MonsterMoves
KoraxLib.VanillaAbilities
```

内部实现位于 `KoraxLib.Internal.*`，不向消费方提供稳定性保证。
