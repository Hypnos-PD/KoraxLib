using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace KoraxLib.Enemies.Behaviour;

/// <summary>
/// 用于定义敌人完整出招状态机的流式构建器。
/// </summary>
/// <remarks>
/// <para>
/// 该状态机图支持三种状态转换方式：
/// </para>
/// <list type="bullet">
///   <item>
///     <term>初始条件分支</term>
///     <description>使用 <see cref="InitialState" /> 和
///       <see cref="InitialStateBuilder" /> 根据运行时条件（例如前排/后排行为）
///       选择第一个出招。</description>
///   </item>
///   <item>
///     <term>确定性循环</term>
///     <description>每个 <see cref="Move" /> 可以通过
///       <see cref="MoveConfigurator.Then" /> 声明唯一的后续出招，形成固定序列。</description>
///   </item>
///   <item>
///     <term>加权随机分支</term>
///     <description>使用 <see cref="RandomBranch" /> 和
///       <see cref="RandomBranchConfigurator" /> 进行非确定性出招选择，
///       支持冷却和重复限制。</description>
///   </item>
/// </list>
/// <para>
/// 该图在规范模型创建时定义一次；每个战斗可变克隆都会从同一份定义
/// 构建一个新的 <c>MonsterMoveStateMachine</c>。
/// </para>
/// </remarks>
public sealed class MoveGraph
{
    private readonly List<MoveConfigurator> _moveConfigs = [];
    private readonly List<BranchConfig> _randomBranches = [];
    private InitialStateConfig? _initialConfig;

    /// <summary>
    /// 定义条件初始状态选择。
    /// </summary>
    /// <remarks>
    /// 初始状态构建器按注册顺序以“首真即胜”的方式评估条件。
    /// 最后一个注册的分支隐式作为兜底分支。
    /// 如果没有任何分支评估为 <c>true</c>，底层 STS2 的
    /// <c>ConditionalBranchState</c> 会抛出 <c>InvalidOperationException</c>。
    /// </remarks>
    public MoveGraph InitialState(Action<InitialStateBuilder> configure)
    {
        var builder = new InitialStateBuilder();
        configure(builder);
        _initialConfig = builder.Build();
        return this;
    }

    /// <summary>
    /// 向状态机注册一个出招。
    /// </summary>
    /// <param name="id">该出招的唯一标识符（被 <see cref="MoveConfigurator.Then" /> 和分支引用）。</param>
    /// <param name="configure">流式配置动作。</param>
    public MoveGraph Move(string id, Action<MoveConfigurator> configure)
    {
        var config = new MoveConfigurator(id);
        configure(config);
        _moveConfigs.Add(config);
        return this;
    }

    /// <summary>
    /// 向状态机注册一个加权随机分支。
    /// </summary>
    /// <param name="id">该分支的唯一标识符。</param>
    /// <param name="configure">流式配置动作。</param>
    public MoveGraph RandomBranch(string id, Action<RandomBranchConfigurator> configure)
    {
        var config = new RandomBranchConfigurator(id);
        configure(config);
        _randomBranches.Add(config.Build());
        return this;
    }

    /// <summary>
    /// 构建并返回为给定拥有者模型配置的 <c>MonsterMoveStateMachine</c>。
    /// </summary>
    /// <remarks>
    /// 由 <see cref="KoraxEnemy.GenerateMoveStateMachine" /> 在可变战斗克隆上调用。
    /// 此时 <c>owner.Creature</c> 已经可用，条件 lambda 可以正确求值。
    /// </remarks>
    internal MonsterMoveStateMachine Build(MonsterModel owner)
    {
        // 1. 从配置器构建 MoveState 实例。
        var moveStates = new Dictionary<string, MoveState>(StringComparer.Ordinal);
        foreach (var mc in _moveConfigs)
        {
            var moveState = mc.BuildMoveState();
            moveStates[mc.Id] = moveState;
        }

        // 2. 连接 FollowUpState 引用（需要两个 Pass，因为 FollowUpStateId 是字符串）。
        foreach (var mc in _moveConfigs)
        {
            if (moveStates.TryGetValue(mc.Id, out var ms) && mc.FollowUpId is not null)
            {
                if (!moveStates.TryGetValue(mc.FollowUpId, out var followUp))
                {
                    throw new InvalidOperationException(
                        $"出招 '{mc.Id}' 引用了未知的后续出招 '{mc.FollowUpId}'。" +
                        "所有后续出招必须先通过 MoveGraph.Move() 声明。");
                }

                ms.FollowUpState = followUp;
            }
        }

        // 3. 收集所有 MonsterState 实例。
        var allStates = new List<MonsterState>();
        allStates.AddRange(moveStates.Values);

        // 4. 构建随机分支状态（通过字符串 ID 连接到出招状态）。
        foreach (var branchConfig in _randomBranches)
        {
            var rb = new RandomBranchState(branchConfig.Id);
            foreach (var entry in branchConfig.Entries)
            {
                if (!moveStates.TryGetValue(entry.MoveId, out var targetState))
                {
                    throw new InvalidOperationException(
                        $"随机分支 '{branchConfig.Id}' 引用了未知的出招 '{entry.MoveId}'。");
                }

                if (entry.MaxRepeats is int maxRepeats)
                {
                    rb.AddBranch(targetState, entry.Cooldown, maxRepeats, () => entry.Weight);
                }
                else
                {
                    rb.AddBranch(targetState, entry.Cooldown, entry.RepeatType, () => entry.Weight);
                }
            }

            allStates.Add(rb);
        }

        // 5. 确定初始状态。
        MonsterState initialState;
        if (_initialConfig is {} ic)
        {
            var cb = new ConditionalBranchState("INIT");
            foreach (var (moveId, condition) in ic.Conditions)
            {
                if (!moveStates.TryGetValue(moveId, out var targetMove))
                {
                    throw new InvalidOperationException(
                        $"初始状态引用了未知的出招 '{moveId}'。" +
                        "所有初始状态出招必须先通过 MoveGraph.Move() 声明。");
                }

                // 闭包捕获 owner 的 Creature 用于运行时求值。
                cb.AddState(targetMove, () => condition(owner.Creature));
            }

            allStates.Add(cb);
            initialState = cb;
        }
        else if (_moveConfigs.Count > 0)
        {
            initialState = moveStates[_moveConfigs[0].Id];
        }
        else
        {
            throw new InvalidOperationException("MoveGraph 必须定义至少一个 Move。");
        }

        return new MonsterMoveStateMachine(allStates, initialState);
    }

    internal sealed record InitialStateConfig(List<(string MoveId, Func<Creature, bool> Condition)> Conditions);

    /// <summary>
    /// 条件初始状态选择的构建器。
    /// </summary>
    public sealed class InitialStateBuilder
    {
        private readonly List<(string MoveId, Func<Creature, bool> Condition)> _conditions = [];

        /// <summary>
        /// 当 <paramref name="condition" /> 为 true 时路由到 <paramref name="moveId" />。
        /// </summary>
        /// <remarks>
        /// 条件按注册顺序以“首真即胜”的方式评估。
        /// 最后一个注册的分支隐式作为兜底 —— 可用 <c>static _ => true</c> 使其显式化。
        /// </remarks>
        public InitialStateBuilder When(Func<Creature, bool> condition, string moveId)
        {
            ArgumentNullException.ThrowIfNull(condition);
            _conditions.Add((moveId, condition));
            return this;
        }

        /// <summary>
        /// 无条件兜底路由（等价于 <c>When(_ => true, moveId)</c>）。
        /// </summary>
        public InitialStateBuilder Else(string moveId)
        {
            _conditions.Add((moveId, static _ => true));
            return this;
        }

        internal InitialStateConfig Build()
        {
            if (_conditions.Count == 0)
            {
                throw new InvalidOperationException("InitialStateBuilder 必须至少有一个条件。");
            }

            return new InitialStateConfig(_conditions);
        }
    }

    internal sealed record BranchConfig(string Id, List<BranchEntry> Entries);

    internal sealed record BranchEntry(
        string MoveId,
        float Weight,
        int Cooldown,
        MoveRepeatType RepeatType,
        int? MaxRepeats);

    /// <summary>
    /// 加权随机分支的构建器。
    /// </summary>
    public sealed class RandomBranchConfigurator
    {
        private readonly string _id;
        private readonly List<BranchEntry> _entries = [];

        internal RandomBranchConfigurator(string id)
        {
            _id = id;
        }

        /// <summary>
        /// 向随机选择池中添加一个具有给定权重的分支。
        /// </summary>
        /// <param name="moveId">选中该分支时要转移到的出招。</param>
        /// <param name="weight">相对权重（越高越容易被选中）。</param>
        /// <param name="repeatType">控制该分支可以重复出现的频率。</param>
        /// <param name="cooldown">该分支再次出现前需要经过的最少其他出招数。</param>
        public RandomBranchConfigurator Branch(
            string moveId,
            float weight = 1f,
            MoveRepeatType repeatType = MoveRepeatType.CanRepeatForever,
            int cooldown = 0)
        {
            _entries.Add(new BranchEntry(moveId, weight, cooldown, repeatType, null));
            return this;
        }

        /// <summary>
        /// 添加一个具有最大重复次数的分支。
        /// </summary>
        /// <param name="moveId">选中该分支时要转移到的出招。</param>
        /// <param name="maxRepeats">该分支最多可以被选中的次数。</param>
        /// <param name="weight">相对权重（越高越容易被选中）。</param>
        /// <param name="cooldown">该分支再次出现前需要经过的最少其他出招数。</param>
        public RandomBranchConfigurator Branch(
            string moveId,
            int maxRepeats,
            float weight = 1f,
            int cooldown = 0)
        {
            _entries.Add(new BranchEntry(moveId, weight, cooldown, MoveRepeatType.CanRepeatXTimes, maxRepeats));
            return this;
        }

        internal BranchConfig Build() => new(_id, _entries);
    }
}
