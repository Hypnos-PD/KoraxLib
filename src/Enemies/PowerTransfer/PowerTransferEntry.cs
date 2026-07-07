namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 一个原版 power 的转移兼容性条目。
/// </summary>
public sealed record PowerTransferEntry
{
    private PowerTransferEntry()
    {
    }

    /// <summary>
    /// 原版 power 类型名。当前以简单类名为稳定键，例如 <c>CurlUpPower</c>。
    /// </summary>
    public required string PowerClassName { get; init; }

    /// <summary>
    /// 可选的原版 <c>PowerModel.Id.Entry</c>。当类型名不足以表达分类时使用。
    /// </summary>
    public string? PowerId { get; init; }

    /// <summary>
    /// 转移安全分类。
    /// </summary>
    public required PowerTransferSafety Safety { get; init; }

    /// <summary>
    /// 兼容适配器键。仅 <see cref="PowerTransferSafety.NeedsAdapter" /> 条目需要。
    /// </summary>
    public string? AdapterKey { get; init; }

    /// <summary>
    /// 人类可读备注，用于解释为什么该条目安全、需要适配或不支持。
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// 创建可直接克隆的条目。
    /// </summary>
    public static PowerTransferEntry SafeClone(string powerClassName, string? powerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(powerClassName);

        return new PowerTransferEntry
        {
            PowerClassName = powerClassName,
            PowerId = powerId,
            Safety = PowerTransferSafety.SafeClone,
        };
    }

    /// <summary>
    /// 创建需要兼容适配器的条目。
    /// </summary>
    public static PowerTransferEntry NeedsAdapter(string powerClassName, string adapterKey, string? powerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(powerClassName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);

        return new PowerTransferEntry
        {
            PowerClassName = powerClassName,
            PowerId = powerId,
            Safety = PowerTransferSafety.NeedsAdapter,
            AdapterKey = adapterKey,
        };
    }

    /// <summary>
    /// 创建不支持转移的条目。
    /// </summary>
    public static PowerTransferEntry Unsupported(string powerClassName, string? powerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(powerClassName);

        return new PowerTransferEntry
        {
            PowerClassName = powerClassName,
            PowerId = powerId,
            Safety = PowerTransferSafety.Unsupported,
        };
    }
}
