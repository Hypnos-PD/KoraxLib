using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace KoraxLib.Enemies.Behaviour;

/// <summary>
/// 便捷基础类，继承自 <c>MonsterModel</c>，使用流式出招状态机构建器。
/// </summary>
/// <remarks>
/// <para>
/// 模组作者继承此类并实现 <see cref="ConfigureMoves" /> 即可定义敌人的出招行为，
/// 无需手动编写 <c>MonsterMoveStateMachine</c> 样板代码。HP、SFX、视觉资源等
/// 属性仍然通过直接重写 <c>MonsterModel</c> 的虚属性来配置（与编写原版敌人相同）。
/// </para>
/// <para>
/// 提供 <see cref="SetFlag" /> / <see cref="GetFlag{T}" /> 用于存储自定义
/// 可变状态（如前排/后排标记、是否孤立等），这些状态在每次 <c>MemberwiseClone</c>
/// 时自动为每个可变克隆重置为独立副本。
/// </para>
/// </remarks>
public abstract class KoraxEnemy : MonsterModel
{
    private MoveGraph? _moveGraph;
    private Dictionary<string, object?>? _runtimeState;

    /// <summary>
    /// 当前可变实例的自定义运行时状态字典。
    /// </summary>
    /// <remarks>
    /// 规范模型上该字典为空；战斗可变克隆在 <see cref="AfterCloned" /> 中
    /// 获得独立副本。
    /// </remarks>
    private Dictionary<string, object?> RuntimeState =>
        _runtimeState ?? throw new InvalidOperationException(
            "RuntimeState 在规范模型上不可用。SetFlag/GetFlag 只能在可变克隆上调用。");

    // ── 子类必须实现 ──────────────────────────────────────────

    /// <summary>
    /// 定义该敌人的出招状态机图。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此方法在每个规范实例上调用一次。图中声明的出招和分支定义会被缓存，
    /// 后续每个战斗可变克隆都会据此构建新的 <c>MonsterMoveStateMachine</c>。
    /// </para>
    /// <para>
    /// 条件 lambda（<see cref="MoveGraph.InitialStateBuilder.When" />）
    /// 在运行时通过 <c>owner.Creature</c> 求值，因此可以访问可变克隆的
    /// <see cref="SetFlag" /> / <see cref="GetFlag{T}" /> 状态。
    /// </para>
    /// </remarks>
    protected abstract void ConfigureMoves(MoveGraph moves);

    // ── 自动实现的方法 ──────────────────────────────────────────

    /// <inheritdoc />
    protected sealed override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _moveGraph ??= BuildMoveGraph();
        return _moveGraph.Build(this);
    }

    // ── 自定义可变状态 ──────────────────────────────────────────

    /// <summary>
    /// 为当前可变实例设置一个具名状态标志。
    /// </summary>
    /// <param name="key">状态标志的唯一名称。</param>
    /// <param name="value">要存储的值（可为 <c>null</c>）。</param>
    /// <exception cref="InvalidOperationException">在规范模型上调用。</exception>
    /// <remarks>
    /// <para>
    /// 典型用法：在 <c>EncounterModel.GenerateMonsters()</c> 中设置初始状态，
    /// 或通过 <see cref="EnemyEvents" /> 钩子在运行时修改状态。
    /// </para>
    /// <code>
    /// // 在 encounter 中：
    /// var frontNibbit = KoraxEnemy.From(monsterClone);
    /// frontNibbit.SetFlag("isFront", true);
    /// </code>
    /// </remarks>
    protected void SetFlag(string key, object? value)
    {
        RuntimeState[key] = value;
    }

    /// <summary>
    /// 从当前可变实例读取一个具名状态标志。
    /// </summary>
    /// <typeparam name="T">期望的值类型。</typeparam>
    /// <param name="key">状态标志的唯一名称。</param>
    /// <param name="defaultValue">当标志不存在时返回的默认值。</param>
    /// <returns>存储的值，或在标志不存在时返回 <paramref name="defaultValue" />。</returns>
    /// <exception cref="InvalidOperationException">在规范模型上调用。</exception>
    protected T? GetFlag<T>(string key, T? defaultValue = default)
    {
        if (RuntimeState.TryGetValue(key, out var raw) && raw is T typed)
        {
            return typed;
        }

        return defaultValue;
    }

    /// <summary>
    /// 检查当前可变实例是否存在指定的状态标志。
    /// </summary>
    protected bool HasFlag(string key)
    {
        return RuntimeState.ContainsKey(key);
    }

    // ── 便捷静态辅助方法 ────────────────────────────────────────

    /// <summary>
    /// 将 <c>MonsterModel</c> 实例安全转换为 <see cref="KoraxEnemy" />。
    /// </summary>
    /// <param name="monster">怪物模型实例（通常是可变克隆）。</param>
    /// <returns>转换后的 <see cref="KoraxEnemy" />。</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="monster" /> 不是 <see cref="KoraxEnemy" /> 子类型。
    /// </exception>
    /// <remarks>
    /// 用于 <c>EncounterModel.GenerateMonsters()</c> 中设置可变克隆的初始状态。
    /// </remarks>
    public static KoraxEnemy From(MonsterModel monster)
    {
        if (monster is KoraxEnemy koraxEnemy)
        {
            return koraxEnemy;
        }

        throw new InvalidOperationException(
            $"MonsterModel '{monster.GetType().FullName}' 不是 KoraxEnemy 的子类型。");
    }

    // ── 内部生命周期 ────────────────────────────────────────────

    /// <summary>
    /// 克隆后重置可变状态，确保每个可变实例拥有独立的运行时状态字典。
    /// </summary>
    /// <remarks>
    /// <c>MemberwiseClone()</c> 会浅拷贝 <c>_runtimeState</c> 引用，
    /// 因此克隆后必须在此处创建新的字典实例。
    /// </remarks>
    protected override void AfterCloned()
    {
        base.AfterCloned();
        _runtimeState = [];
    }

    private MoveGraph BuildMoveGraph()
    {
        var graph = new MoveGraph();
        ConfigureMoves(graph);
        return graph;
    }
}
