namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 兼容适配器元数据接口，用于描述某个原版 power 的替代实现。
/// </summary>
/// <remarks>
/// 当前阶段只定义查询契约，不负责创建或应用替代 power。实际运行时转移会在后续阶段接入。
/// </remarks>
public interface IPowerTransferAdapter
{
    /// <summary>
    /// 此适配器处理的原版 power 类型名。
    /// </summary>
    string AdaptedPowerClassName { get; }

    /// <summary>
    /// 替代实现的 power 类型名。
    /// </summary>
    string ReplacementPowerClassName { get; }
}
