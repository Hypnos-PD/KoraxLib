using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace KoraxLib.Enemies.Behaviour;

/// <summary>
/// 单个出招状态的流式配置器。
/// </summary>
/// <remarks>
/// 每个出招接受零个或多个 <see cref="AbstractIntent" /> 实例用于声明式 UI 显示，
/// 以及一个 <c>OnPerform</c> 异步委托执行实际游戏逻辑。
/// <para />
/// 示例：
/// <code>
/// move.Intent(new SingleAttackIntent(6))
///     .OnPerform(async targets => {
///         await DamageCmd.Attack(6).FromMonster(this).Execute(null);
///     })
///     .Then("nextMove");
/// </code>
/// </remarks>
public sealed class MoveConfigurator
{
    private readonly List<AbstractIntent> _intents = [];
    private Func<IReadOnlyList<Creature>, Task> _perform = static _ => Task.CompletedTask;
    private string? _followUpId;
    private bool _requireCompletion;

    /// <summary>
    /// 该出招在状态机中的唯一标识符。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 通过 <see cref="Then" /> 设置的确定性后续出招标识符。
    /// </summary>
    internal string? FollowUpId => _followUpId;

    internal MoveConfigurator(string id)
    {
        Id = id;
    }

    /// <summary>
    /// 添加一个意图图标，在该出招执行前显示在怪物上方。
    /// </summary>
    /// <remarks>
    /// 对于显示多个意图图标的出招（如同时攻击和防御的出招），可多次调用。
    /// </remarks>
    public MoveConfigurator Intent(AbstractIntent intent)
    {
        _intents.Add(intent);
        return this;
    }

    /// <summary>
    /// 设置怪物执行该出招时的异步动作。
    /// </summary>
    /// <param name="perform">
    /// 委托接收该出招的有效目标列表。
    /// 在委托体内使用原版命令构建器（<c>DamageCmd</c>、<c>CreatureCmd</c>、
    /// <c>PowerCmd</c>）。
    /// </param>
    public MoveConfigurator OnPerform(Func<IReadOnlyList<Creature>, Task> perform)
    {
        ArgumentNullException.ThrowIfNull(perform);
        _perform = perform;
        return this;
    }

    /// <summary>
    /// 设置该出招之后确定性地接续的下一个出招。
    /// </summary>
    /// <param name="nextMoveId">下一个出招的 <see cref="Id" />。</param>
    public MoveConfigurator Then(string nextMoveId)
    {
        _followUpId = nextMoveId;
        return this;
    }

    /// <summary>
    /// 要求该出招至少完成一次后，状态机才能转移到其他状态。
    /// </summary>
    /// <remarks>
    /// 用于两回合或多回合出招，其中第二个阶段必须在状态机转移到后续状态之前播放。
    /// </remarks>
    public MoveConfigurator RequireCompletion()
    {
        _requireCompletion = true;
        return this;
    }

    /// <summary>
    /// 构建并返回一个 <see cref="MoveState" /> 用于注册到状态机中。
    /// </summary>
    internal MoveState BuildMoveState()
    {
        return new MoveState(Id, _perform, _intents.ToArray())
        {
            FollowUpStateId = _followUpId,
            MustPerformOnceBeforeTransitioning = _requireCompletion,
        };
    }
}
