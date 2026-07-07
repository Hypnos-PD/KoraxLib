# Getting Started

KoraxLib is a Slay the Spire 2 library mod. It currently focuses on enemy content registration and enemy lifecycle events.

## Prerequisites

- Slay the Spire 2 installed locally.
- .NET SDK 9.0, matching `global.json`.
- The STS2 managed assemblies available through your local game install.

## Configure Local Paths

Copy `local.props.template` to `local.props` in the repository root, then set your local game paths. On Linux the current local setup uses paths like:

```xml
<Project>
  <PropertyGroup>
    <Sts2Dir>/home/you/.local/share/Steam/steamapps/common/Slay the Spire 2</Sts2Dir>
    <Sts2DataDir>$(Sts2Dir)/data_sts2_linuxbsd_x86_64</Sts2DataDir>
  </PropertyGroup>
</Project>
```

The project references `sts2.dll` and `0Harmony.dll` from `$(Sts2DataDir)`.

## Build

From the repository root:

```bash
dotnet build KoraxLib.sln
```

The project build target copies `KoraxLib.dll` and `mod_manifest.json` into the STS2 mods folder configured by `Sts2Dir`.

## Verify The Mod Loads

Start STS2 with mods enabled and check that KoraxLib appears in the mod list. The expected initialization log is:

```text
KoraxLib v0.1.0 initialized.
```

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
