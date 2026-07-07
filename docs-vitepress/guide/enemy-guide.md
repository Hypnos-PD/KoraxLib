# Enemy Guide

Use `EnemyRegistry` during mod initialization to declare enemy-related content. KoraxLib records the declarations while registration is open, then applies them to STS2 model and encounter structures during the safe initialization phase.

## Register A Monster

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterMonster<MyMonsterModel>();
```

The type must be a concrete closed subtype of `MonsterModel`.

## Register An Act Encounter

```csharp
using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Models;

EnemyRegistry.RegisterActEncounter<Overgrowth, MyEncounterModel>();
```

Act-scoped encounters are appended after vanilla encounters for the target act. KoraxLib de-duplicates by `EncounterModel.Id`.

## Register A Global Encounter

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterGlobalEncounter<MyEncounterModel>();
```

Global encounters are appended to every supported act encounter list after act-scoped entries.

## Registration Lifecycle

`EnemyRegistry.State` exposes the current registration state:

```csharp
public enum KoraxRegistrationState
{
    Open,
    Frozen,
}
```

Register content while the state is `Open`. Once KoraxLib freezes registration, later content registration throws `InvalidOperationException`.

Runtime event subscription is separate from content registration and remains available after freeze.

## Duplicate Registrations

Registering the same type more than once is a no-op. KoraxLib logs the duplicate at debug level and keeps the first declaration.

## Save Serialization

KoraxLib patches `ModelIdSerializationCache.Init` so registered monster and encounter IDs are added to STS2's save/network serialization cache. The smoke encounter has verified that `ENCOUNTER.KORAX_SMOKE_ENCOUNTER` can be saved without being written as `NONE`.
