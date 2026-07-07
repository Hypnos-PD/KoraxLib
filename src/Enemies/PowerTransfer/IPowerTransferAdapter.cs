namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 兼容适配器接口，用于处理需要替代实现的原版 power。
/// </summary>
/// <remarks>
/// 适配器负责创建并应用自己的替代 power，通常会调用原版 <c>PowerCmd.Apply</c>。
/// KoraxLib 只负责在 <see cref="PowerTransferService.TransferAsync" /> 中把 <see cref="PowerTransferSafety.NeedsAdapter" />
/// 请求路由给已注册适配器。
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

    /// <summary>
    /// 执行一次兼容转移。
    /// </summary>
    /// <remarks>
    /// 若请求设置了 <see cref="PowerTransferRequest.RemoveSource" />，适配器应在成功应用替代 power 后移除源 power。
    /// </remarks>
    Task<PowerTransferResult> TransferAsync(PowerTransferRequest request, PowerTransferEntry entry);
}
