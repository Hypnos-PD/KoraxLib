# Milestone 1 规格说明

本页概述 `docs/spec.md`。在 M1 稳定前，源 spec 仍是实现合同。

## 已实现区域

- 注册生命周期：`Open` 到 `Frozen`。
- `EnemyRegistry` 公共注册 API。
- Registered monster / encounter 接入 STS2 model 和 encounter lists。
- Registered model ID 序列化缓存 patch。
- Enemy contexts。
- Enemy lifecycle events。
- Enemy plugin 注册与分发。

## 计划中区域

- Vanilla ability registry。
- Vanilla ability preview。
- Vanilla ability executor。
- Primitive abilities。
- 第一批手写 vanilla ability catalog。

## 测试期望

M1 期望在可行处添加单元测试，并至少用一个 runtime smoke test 覆盖用户可见行为。当前已验证的 runtime smoke 路径覆盖 smoke encounter、lifecycle logs、胜利结算和存档序列化。
