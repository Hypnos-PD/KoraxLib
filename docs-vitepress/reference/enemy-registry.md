# EnemyRegistry

`EnemyRegistry` is the public entry point for declaring monster and encounter content.

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

`State` mirrors KoraxLib's global registration gate. Register content while it is `Open`; registration after `Frozen` throws `InvalidOperationException`.

## Registered Snapshots

`RegisteredMonsters`, `RegisteredGlobalEncounters`, and `RegisteredActEncounters` return defensive snapshots. Mutating the returned collection does not mutate the internal registry.

## Type Requirements

- Monster types must be concrete closed `MonsterModel` subtypes.
- Act types must be concrete closed `ActModel` subtypes.
- Encounter types must be concrete closed `EncounterModel` subtypes.
- `null`, abstract, interface, and open generic types are rejected.

## Merge Order

Generated encounter lists are merged in this order:

1. Vanilla encounters.
2. Act-scoped registered encounters.
3. Global registered encounters.

KoraxLib de-duplicates by `EncounterModel.Id`.
