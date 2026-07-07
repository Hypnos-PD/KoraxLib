# 开始使用

KoraxLib 是一个 Slay the Spire 2 前置库模组。目前重点是敌人内容注册和敌人生命周期事件。

## 前置条件

- 本地已安装 Slay the Spire 2。
- .NET SDK 9.0，版本与 `global.json` 匹配。
- 本地游戏目录中可访问 STS2 managed assemblies。

## 配置本地路径

把仓库根目录的 `local.props.template` 复制为 `local.props`，然后设置本地游戏路径。Linux 环境可以类似这样配置：

```xml
<Project>
  <PropertyGroup>
    <Sts2Dir>/home/you/.local/share/Steam/steamapps/common/Slay the Spire 2</Sts2Dir>
    <Sts2DataDir>$(Sts2Dir)/data_sts2_linuxbsd_x86_64</Sts2DataDir>
  </PropertyGroup>
</Project>
```

项目会从 `$(Sts2DataDir)` 引用 `sts2.dll` 和 `0Harmony.dll`。

## 构建

在仓库根目录运行：

```bash
dotnet build KoraxLib.sln
```

构建目标会把 `KoraxLib.dll` 和 `mod_manifest.json` 复制到 `Sts2Dir` 配置的 STS2 mods 目录。

## 验证模组加载

启用 mods 启动 STS2，并确认模组列表里出现 KoraxLib。预期初始化日志是：

```text
KoraxLib v0.1.0 initialized.
```

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
