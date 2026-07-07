namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 原版 enemy power 转移到其他 creature 时的安全分类。
/// </summary>
public enum PowerTransferSafety
{
    /// <summary>
    /// 可直接克隆或复用，不依赖特定原版敌人/遭遇上下文。
    /// </summary>
    SafeClone,

    /// <summary>
    /// 需要消费模组提供兼容替代实现后才应转移。
    /// </summary>
    NeedsAdapter,

    /// <summary>
    /// 不应转移。通常表示该 power 深度依赖敌人生命周期、遭遇状态或私有运行时状态。
    /// </summary>
    Unsupported,
}
