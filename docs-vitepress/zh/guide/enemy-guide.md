# 敌人与遭遇

消费模组在初始化期间通过 `EnemyRegistry` 声明敌人相关内容。KoraxLib 会在注册窗口开放时记录这些声明，并在安全的初始化阶段把它们应用到 STS2 model 和 encounter 结构中。

## 注册 Monster

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterMonster<MyMonsterModel>();
```

类型必须是 concrete closed `MonsterModel` 子类型。

## 注册 Act Encounter

```csharp
using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Models;

EnemyRegistry.RegisterActEncounter<Overgrowth, MyEncounterModel>();
```

Act-scoped encounters 会追加到目标 act 的 vanilla encounters 后面。KoraxLib 按 `EncounterModel.Id` 去重。

## 注册 Global Encounter

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterGlobalEncounter<MyEncounterModel>();
```

Global encounters 会追加到所有支持 act 的 encounter list 中，顺序在 act-scoped entries 之后。

## 注册生命周期

`EnemyRegistry.State` 暴露当前注册状态：

```csharp
public enum KoraxRegistrationState
{
    Open,
    Frozen,
}
```

内容注册必须发生在 `Open` 状态。KoraxLib 冻结注册后，再注册内容会抛出 `InvalidOperationException`。

运行时事件订阅不属于内容注册；freeze 后仍然允许。

## 重复注册

重复注册同一类型是 no-op。KoraxLib 会用 debug 日志记录重复声明，并保留第一次声明。

## 存档序列化

KoraxLib patch 了 `ModelIdSerializationCache.Init`，把 registered monster / encounter IDs 补入 STS2 的存档和网络序列化缓存。Smoke encounter 已验证 `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` 可以保存，不会被写成 `NONE`。
