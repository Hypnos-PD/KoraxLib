using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 原版 enemy power 转移兼容性查询与安全克隆入口。
/// </summary>
/// <remarks>
/// <see cref="PowerTransferSafety.SafeClone" /> 会走内置克隆路径。
/// <see cref="PowerTransferSafety.NeedsAdapter" /> 会路由给 <see cref="PowerTransferAdapterRegistry" /> 中注册的适配器。
/// </remarks>
public static class PowerTransferService
{
    /// <summary>
    /// 按目录分类执行一次 power 转移。
    /// </summary>
    /// <remarks>
    /// 该方法会在执行 safe-clone 或 adapter 前检查数量和目标接收能力，避免原版命令静默 no-op 后仍移除源 power。
    /// </remarks>
    public static async Task<PowerTransferResult> TransferAsync(PowerTransferRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ChoiceContext);
        ArgumentNullException.ThrowIfNull(request.SourcePower);
        ArgumentNullException.ThrowIfNull(request.Target);

        PowerTransferCatalogue.TryGetEntry(request.SourcePower, out var entry);
        var safety = entry?.Safety ?? PowerTransferSafety.Unsupported;

        if (safety == PowerTransferSafety.Unsupported)
        {
            return PowerTransferResult.Skipped(PowerTransferStatus.Unsupported, safety, entry);
        }

        if (request.SourcePower.Amount <= 0)
        {
            return PowerTransferResult.Skipped(PowerTransferStatus.EmptyAmount, safety, entry);
        }

        if (!request.Target.CanReceivePowers || request.Target.CombatState is null)
        {
            return PowerTransferResult.Skipped(PowerTransferStatus.TargetCannotReceive, safety, entry);
        }

        if (safety == PowerTransferSafety.NeedsAdapter)
        {
            if (entry is not null && PowerTransferAdapterRegistry.TryGet(entry.PowerClassName, out var adapter))
            {
                return await adapter.TransferAsync(request, entry);
            }

            return PowerTransferResult.Skipped(PowerTransferStatus.AdapterRequired, safety, entry);
        }

        if (request.SourcePower.ClonePreservingMutability() is not PowerModel copiedPower)
        {
            return PowerTransferResult.Skipped(PowerTransferStatus.CloneFailed, safety, entry);
        }

        await PowerCmd.Apply(
            request.ChoiceContext,
            copiedPower,
            request.Target,
            request.SourcePower.Amount,
            request.Applier,
            request.CardSource,
            request.Silent);

        if (!request.RemoveSource)
        {
            return PowerTransferResult.Applied(safety, entry, sourceRemoved: false);
        }

        await PowerCmd.Remove(request.SourcePower);
        return PowerTransferResult.Applied(safety, entry, sourceRemoved: true);
    }

    /// <summary>
    /// 判断指定 power 模型实例是否允许进入转移流程。
    /// </summary>
    public static bool CanTransfer(PowerModel power)
    {
        return IsTransferable(PowerTransferCatalogue.Classify(power));
    }

    /// <summary>
    /// 判断指定 power 类型是否允许进入转移流程。
    /// </summary>
    public static bool CanTransfer(Type powerType)
    {
        return IsTransferable(PowerTransferCatalogue.Classify(powerType));
    }

    /// <summary>
    /// 判断指定 power 类型名是否允许进入转移流程。
    /// </summary>
    public static bool CanTransfer(string powerClassName)
    {
        return IsTransferable(PowerTransferCatalogue.Classify(powerClassName));
    }

    /// <summary>
    /// 查询指定 power 模型实例的转移安全分类。
    /// </summary>
    public static PowerTransferSafety ClassifyPower(PowerModel power)
    {
        return PowerTransferCatalogue.Classify(power);
    }

    /// <summary>
    /// 查询指定 power 类型的转移安全分类。
    /// </summary>
    public static PowerTransferSafety ClassifyPower(Type powerType)
    {
        return PowerTransferCatalogue.Classify(powerType);
    }

    /// <summary>
    /// 查询指定 power 类型名的转移安全分类。
    /// </summary>
    public static PowerTransferSafety ClassifyPower(string powerClassName)
    {
        return PowerTransferCatalogue.Classify(powerClassName);
    }

    /// <summary>
    /// 尝试获取指定 power 模型实例的目录条目。
    /// </summary>
    public static bool TryGetEntry(PowerModel power, out PowerTransferEntry entry)
    {
        return PowerTransferCatalogue.TryGetEntry(power, out entry);
    }

    /// <summary>
    /// 尝试获取指定 power 类型的目录条目。
    /// </summary>
    public static bool TryGetEntry(Type powerType, out PowerTransferEntry entry)
    {
        return PowerTransferCatalogue.TryGetEntry(powerType, out entry);
    }

    private static bool IsTransferable(PowerTransferSafety safety)
    {
        return safety is PowerTransferSafety.SafeClone or PowerTransferSafety.NeedsAdapter;
    }
}
