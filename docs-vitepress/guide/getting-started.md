# Getting Started

KoraxLib is a Slay the Spire 2 library mod. It currently focuses on enemy content registration and enemy lifecycle events.

This guide is for mod authors who want to consume KoraxLib from their own mod. You do not need to copy the KoraxLib repository into your project.

## Prerequisites

- Slay the Spire 2 installed locally.
- A C# STS2 mod project.
- .NET SDK 9.0, or the SDK version your STS2 mod project uses.
- The STS2 managed assemblies available through your local game install.
- KoraxLib installed as a mod alongside your mod.

## Install KoraxLib

Install KoraxLib into the STS2 `mods` directory the same way you install other library mods. The installed folder should contain:

```text
KoraxLib.dll
mod_manifest.json
```

Start STS2 with mods enabled and confirm KoraxLib appears in the mod list. The expected initialization log is:

```text
KoraxLib v0.1.0 initialized.
```

## Reference KoraxLib From Your Mod

In your own mod project, reference `KoraxLib.dll` from the installed mod folder or from your local development copy. Keep `Private="False"` so your mod does not bundle a second copy of the library.

```xml
<ItemGroup>
  <Reference Include="KoraxLib" HintPath="$(Sts2Dir)\mods\KoraxLib\KoraxLib.dll" Private="False" />
</ItemGroup>
```

Your mod project still needs its normal STS2 references, for example:

```xml
<ItemGroup>
  <Reference Include="sts2" HintPath="$(Sts2DataDir)\sts2.dll" Private="False" />
  <Reference Include="0Harmony" HintPath="$(Sts2DataDir)\0Harmony.dll" Private="False" />
</ItemGroup>
```

Use whatever path convention your mod project already uses for `$(Sts2Dir)` and `$(Sts2DataDir)`.

## Use The API

Call KoraxLib APIs from your own mod initialization code:

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterMonster<MyMonsterModel>();
EnemyRegistry.RegisterActEncounter<MyActModel, MyEncounterModel>();
```

See [Enemy Guide](./enemy-guide.md) for registration details.

## For KoraxLib Contributors

If you are contributing to KoraxLib itself, clone this repository and use the repository-local build flow:

```bash
dotnet build KoraxLib.sln
```

That contributor workflow uses `local.props.template` and the repository `global.json`. It is not required for normal KoraxLib consumers.

## What Works Today

- Register monster model types.
- Register act-scoped and global encounter model types.
- Observe enemy lifecycle events.
- Register `IEnemyPlugin` instances.
- Run the opt-in smoke encounter and lifecycle logger.

## What Is Still Planned

- Vanilla ability registry, preview, executor, and catalog.
- Monster authoring DSL.
- Visual and animation helpers.
- Compatibility adapters for other library mods.

Next: [Enemy Guide](./enemy-guide.md).
