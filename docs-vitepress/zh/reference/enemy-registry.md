# EnemyRegistry

`EnemyRegistry` 是声明 monster 和 encounter 内容的公共入口。

```csharp
namespace KoraxLib.Enemies;

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

## State

`State` 反映 KoraxLib 全局注册闸门。内容应在 `Open` 状态注册；`Frozen` 后继续注册会抛出 `InvalidOperationException`。

## 注册快照

`RegisteredMonsters`、`RegisteredGlobalEncounters` 和 `RegisteredActEncounters` 返回防御性快照。修改返回集合不会修改内部 registry。

## 类型要求

- Monster 类型必须是 concrete closed `MonsterModel` 子类型。
- Act 类型必须是 concrete closed `ActModel` 子类型。
- Encounter 类型必须是 concrete closed `EncounterModel` 子类型。
- `null`、abstract、interface 和 open generic 类型会被拒绝。

## 合并顺序

Generated encounter lists 按以下顺序合并：

1. Vanilla encounters。
2. Act-scoped registered encounters。
3. Global registered encounters。

KoraxLib 按 `EncounterModel.Id` 去重。
