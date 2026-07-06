namespace KoraxLib;

/// <summary>
/// KoraxLib 内容注册表的生命周期状态。
/// </summary>
/// <remarks>
/// 这个枚举是公开类型，因为后续 `EnemyRegistry.State`、`VanillaAbilityRegistry.State`
/// 这类公共 API 会把它返回给消费端。具体什么时候切换状态仍由 KoraxLib 内部控制，
/// 消费端只能读取状态，不能主动冻结或重新打开注册窗口。
/// </remarks>
public enum KoraxRegistrationState
{
    /// <summary>
    /// 注册窗口仍然开放。模组可以声明自己的 enemy、encounter 或其他内容。
    /// </summary>
    Open,

    /// <summary>
    /// 注册窗口已经关闭。KoraxLib 正在或已经把收集到的内容合并进 STS2。
    /// </summary>
    Frozen,
}
