namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 一次 power 转移请求的结果。
/// </summary>
public sealed record PowerTransferResult
{
    /// <summary>
    /// 执行结果状态。
    /// </summary>
    public required PowerTransferStatus Status { get; init; }

    /// <summary>
    /// 源 power 的安全分类。
    /// </summary>
    public required PowerTransferSafety Safety { get; init; }

    /// <summary>
    /// 实际用于决策的目录条目。
    /// </summary>
    public PowerTransferEntry? Entry { get; init; }

    /// <summary>
    /// 是否已移除源 power。
    /// </summary>
    public bool SourceRemoved { get; init; }

    /// <summary>
    /// 可读说明，通常用于日志。
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 创建成功执行的结果。
    /// </summary>
    public static PowerTransferResult Applied(PowerTransferSafety safety, PowerTransferEntry? entry, bool sourceRemoved)
    {
        return new PowerTransferResult
        {
            Status = PowerTransferStatus.Applied,
            Safety = safety,
            Entry = entry,
            SourceRemoved = sourceRemoved,
        };
    }

    /// <summary>
    /// 创建未执行转移的结果。
    /// </summary>
    public static PowerTransferResult Skipped(PowerTransferStatus status, PowerTransferSafety safety, PowerTransferEntry? entry)
    {
        return new PowerTransferResult
        {
            Status = status,
            Safety = safety,
            Entry = entry,
            SourceRemoved = false,
        };
    }
}
