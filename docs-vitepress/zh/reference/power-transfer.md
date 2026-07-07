# Power Transfer

`KoraxLib.Enemies.PowerTransfer` 提供一个共享的兼容性目录，用于需要检查或转移原版敌人 power 的模组。

该模块的初始数据来自 STSVWB `AchimPowerTransferMapper` 对“绝望之王·阿基姆”偷取敌方 Buff 的兼容性研究：哪些 power 可以安全克隆，哪些需要玩家侧替代实现，哪些因为依赖敌人生命周期或遭遇状态而不应转移。

::: warning 适配器路径尚未实现
`SafeClone` power 现在可以在运行时转移。`NeedsAdapter` power 仍然只返回 `AdapterRequired`，在运行时适配器实现前不会修改战斗状态。
:::

## 入口

```csharp
using KoraxLib.Enemies.PowerTransfer;

PowerTransferSafety safety = PowerTransferService.ClassifyPower("SkittishPower");
bool canTransfer = PowerTransferService.CanTransfer("AfterimagePower");

if (PowerTransferService.TryGetEntry("CurlUpPower", out var entry))
{
    var adapterKey = entry.AdapterKey;
}
```

## 运行时 SafeClone 转移

`TransferAsync` 只执行 `SafeClone` 路径。它会在调用 STS2 `PowerCmd.Apply` 前确认源 power 数量为正、目标可以接收 power。`RemoveSource` 为 `true` 时，会在发出 apply 命令后移除源 power。

```csharp
PowerTransferResult result = await PowerTransferService.TransferAsync(new PowerTransferRequest
{
    ChoiceContext = choiceContext,
    SourcePower = randomBuff,
    Target = Owner.Creature,
    Applier = Owner.Creature,
    CardSource = this,
    RemoveSource = true,
});

if (result.Status == PowerTransferStatus.AdapterRequired)
{
    // 先提供兼容替代实现，再转移该 power。
}
```

`TransferAsync` 不会应用 `NeedsAdapter` 或 `Unsupported` power。这些路径会返回结构化结果，并保持战斗状态不变。

## PowerTransferStatus

| 值 | 含义 |
| --- | --- |
| `Applied` | 已执行 safe-clone 路径。 |
| `AdapterRequired` | 该 power 需要兼容替代实现。 |
| `Unsupported` | 该 power 被分类为不支持或未知。 |
| `EmptyAmount` | 源 power 数量为零或负数。 |
| `TargetCannotReceive` | 目标当前不能接收 power。 |
| `CloneFailed` | `ClonePreservingMutability()` 未产生 `PowerModel`。 |

## PowerTransferSafety

| 值 | 含义 |
| --- | --- |
| `SafeClone` | 可以进入通用克隆路径。 |
| `NeedsAdapter` | 只有通过兼容替代实现才可转移。 |
| `Unsupported` | 不应转移。未知 power 默认归入此类。 |

## PowerTransferCatalogue

`PowerTransferCatalogue` 持有初始数据，并允许在 KoraxLib 注册窗口开放时追加条目：

```csharp
PowerTransferCatalogue.Register(
    PowerTransferEntry.NeedsAdapter("MyEnemyOnlyPower", "MY_ENEMY_ONLY_POWER"));
```

已知条目以简单类名为键，例如 `CurlUpPower`。字符串查询也支持完整限定名，KoraxLib 会规范化为最后的类名片段。

## 初始数据

当前可安全克隆的种子包含 STSVWB Achim 白名单中的 power，例如 `AfterimagePower`、`BarricadePower`、`DexterityPower`、`PlatingPower`、`StormPower`、`VigorPower`、`OrbitPower` 和 `HardenedShellPower`。

当前需要适配器的种子：

| Power | Adapter Key | 原因 |
| --- | --- | --- |
| `CurlUpPower` | `CURL_UP_POWER` | 原版实现会写入 `LouseProgenitor.Curled`。 |
| `SkittishPower` | `SKITTISH_POWER` | 原版实现依赖敌方动画和每回合状态。 |
| `StockPower` | `STOCK_POWER` | 原版库存行为与敌方死亡替换逻辑绑定。 |

不支持种子包括 `HatchPower`、`InterceptPower`、`ReattachPower`、`SlumberPower`、`ThieveryPower`、`VoidFormPower` 等敌人生命周期 power。

## 适配器契约

`IPowerTransferAdapter` 目前仅提供元数据：

```csharp
public interface IPowerTransferAdapter
{
    string AdaptedPowerClassName { get; }
    string ReplacementPowerClassName { get; }
}
```

运行时适配器创建仍然是后续工作。`IPowerTransferAdapter` 只是记录未来 API 的形状，不会假装当前已经能创建替代 power。
