using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 原版 enemy power 转移兼容性查询入口。
/// </summary>
/// <remarks>
/// 当前阶段只负责分类与可转移性判断；实际克隆、替代 power 创建和移除原 power 的运行时流程将在后续阶段实现。
/// </remarks>
public static class PowerTransferService
{
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
