using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 一次 power 转移请求的完整输入。
/// </summary>
public sealed record PowerTransferRequest
{
    /// <summary>
    /// 原版命令系统使用的玩家选择上下文。
    /// </summary>
    public required PlayerChoiceContext ChoiceContext { get; init; }

    /// <summary>
    /// 准备转移的源 power。
    /// </summary>
    public required PowerModel SourcePower { get; init; }

    /// <summary>
    /// 接收克隆 power 的目标 creature。
    /// </summary>
    public required Creature Target { get; init; }

    /// <summary>
    /// 应用者 creature，传给原版 <c>PowerCmd.Apply</c>。
    /// </summary>
    public Creature? Applier { get; init; }

    /// <summary>
    /// 来源卡牌，传给原版 <c>PowerCmd.Apply</c>。
    /// </summary>
    public CardModel? CardSource { get; init; }

    /// <summary>
    /// 成功执行安全克隆路径后，是否移除源 power。
    /// </summary>
    public bool RemoveSource { get; init; } = true;

    /// <summary>
    /// 是否静默应用目标 power。
    /// </summary>
    public bool Silent { get; init; }
}
