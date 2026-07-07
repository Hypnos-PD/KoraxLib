# 开始使用

KoraxLib 是一个 Slay the Spire 2 前置库模组。目前重点是敌人内容注册和敌人生命周期事件。

本指南面向想在自己模组中使用 KoraxLib 的模组作者。你不需要把 KoraxLib 仓库复制进自己的项目。

## 前置条件

- 本地已安装 Slay the Spire 2。
- 一个 C# STS2 模组项目。
- .NET SDK 9.0，或你的 STS2 模组项目使用的 SDK 版本。
- 本地游戏目录中可访问 STS2 managed assemblies。
- KoraxLib 已作为模组安装，并和你的模组一起启用。

## 安装 KoraxLib

把 KoraxLib 安装到 STS2 的 `mods` 目录，方式和其它前置库模组相同。安装后的目录应包含：

```text
KoraxLib.dll
mod_manifest.json
```

启用 mods 启动 STS2，并确认模组列表中出现 KoraxLib。预期初始化日志是：

```text
KoraxLib v0.1.0 initialized.
```

## 在你的模组中引用 KoraxLib

在你自己的模组项目里，从已安装的 KoraxLib 模组目录或本地开发副本引用 `KoraxLib.dll`。建议保持 `Private="False"`，避免你的模组再打包一份 KoraxLib。

```xml
<ItemGroup>
  <Reference Include="KoraxLib" HintPath="$(Sts2Dir)\mods\KoraxLib\KoraxLib.dll" Private="False" />
</ItemGroup>
```

你的模组项目仍然需要正常引用 STS2 assemblies，例如：

```xml
<ItemGroup>
  <Reference Include="sts2" HintPath="$(Sts2DataDir)\sts2.dll" Private="False" />
  <Reference Include="0Harmony" HintPath="$(Sts2DataDir)\0Harmony.dll" Private="False" />
</ItemGroup>
```

`$(Sts2Dir)` 和 `$(Sts2DataDir)` 使用你自己的模组项目已有路径约定即可。

## 使用 API

在你自己的模组初始化代码中调用 KoraxLib API：

```csharp
using KoraxLib.Enemies;

EnemyRegistry.RegisterMonster<MyMonsterModel>();
EnemyRegistry.RegisterActEncounter<MyActModel, MyEncounterModel>();
```

注册细节见 [敌人与遭遇](./enemy-guide.md)。

## KoraxLib 贡献者

如果你是在贡献 KoraxLib 本身，则 clone 本仓库并使用仓库本地构建流程：

```bash
dotnet build KoraxLib.sln
```

这个贡献者流程会用到 `local.props.template` 和仓库里的 `global.json`。普通 KoraxLib 使用者不需要走这条流程。

## 当前可用能力

- 注册 monster model 类型。
- 注册 act-scoped 和 global encounter model 类型。
- 观察敌人生命周期事件。
- 注册 `IEnemyPlugin` 实例。
- 运行 opt-in smoke encounter 和 lifecycle logger。

## 仍在计划中的能力

- Vanilla ability registry、preview、executor 和 catalog。
- Monster authoring DSL。
- Visual 和 animation helpers。
- 与其它前置库的 compatibility adapters。

下一步：[敌人与遭遇](./enemy-guide.md)。
