namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 一次 power 转移请求的执行结果状态。
/// </summary>
public enum PowerTransferStatus
{
    /// <summary>
    /// 已执行安全克隆路径，并调用原版 <c>PowerCmd.Apply</c>。
    /// </summary>
    Applied,

    /// <summary>
    /// 该 power 需要适配器，当前未执行运行时替代。
    /// </summary>
    AdapterRequired,

    /// <summary>
    /// 该 power 不支持转移。
    /// </summary>
    Unsupported,

    /// <summary>
    /// 源 power 的数量为 0 或负数，不执行转移。
    /// </summary>
    EmptyAmount,

    /// <summary>
    /// 目标当前不能接收 power。
    /// </summary>
    TargetCannotReceive,

    /// <summary>
    /// 克隆源 power 失败。
    /// </summary>
    CloneFailed,
}
