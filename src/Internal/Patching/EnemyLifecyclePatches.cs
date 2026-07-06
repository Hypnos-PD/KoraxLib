using HarmonyLib;
using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;

namespace KoraxLib.Internal.Patching;

/// <summary>
/// 把 STS2 原生 <see cref="Hook" /> 生命周期 hook 桥接到 KoraxLib 的 <see cref="EnemyEvents" />。
/// </summary>
[HarmonyPatch]
internal static class EnemyLifecyclePatches
{
    /// <summary>
    /// 在 STS2 完成 creature 加入战斗 hook 后发布敌人 spawn 事件。
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCreatureAddedToCombat))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void PublishEnemySpawnedAfterHook(ICombatState combatState, Creature creature, ref Task __result)
    {
        __result = HookTaskBridge.After(__result, () => DispatchEnemySpawnedAsync(combatState, creature));
    }

    /// <summary>
    /// 在 STS2 敌方回合开始 hook 前，对 participants 中的敌人发布 turn starting 事件。
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void PublishEnemyTurnStartingBeforeHook(
        ICombatState combatState,
        CombatSide side,
        IReadOnlyList<Creature> participants)
    {
        if (side != CombatSide.Enemy)
        {
            return;
        }

        HookTaskBridge.RunDetached(
            () => DispatchEnemyTurnStartingAsync(combatState, participants),
            nameof(EnemyEvents.EnemyTurnStarting));
    }

    /// <summary>
    /// 在 STS2 敌方回合开始 hook 完成后，对 participants 中的敌人发布 turn started 事件。
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void PublishEnemyTurnStartedAfterHook(
        ICombatState combatState,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ref Task __result)
    {
        if (side != CombatSide.Enemy)
        {
            return;
        }

        __result = HookTaskBridge.After(__result, () => DispatchEnemyTurnStartedAsync(combatState, participants));
    }

    /// <summary>
    /// 在 STS2 死亡判定前发布敌人 dying 事件。
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDeath))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void PublishEnemyDyingBeforeHook(IRunState runState, ICombatState? combatState, Creature creature)
    {
        HookTaskBridge.RunDetached(
            () => DispatchEnemyDyingAsync(runState, combatState, creature),
            nameof(EnemyEvents.EnemyDying));
    }

    /// <summary>
    /// 在 STS2 死亡 hook 完成后发布敌人 died 事件。
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void PublishEnemyDiedAfterHook(
        IRunState runState,
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength,
        ref Task __result)
    {
        __result = HookTaskBridge.After(
            __result,
            () => DispatchEnemyDiedAsync(runState, combatState, creature, wasRemovalPrevented, deathAnimLength));
    }

    private static Task DispatchEnemySpawnedAsync(ICombatState combatState, Creature creature)
    {
        var context = EnemyContext.TryCreate(combatState, creature);
        return context is null ? Task.CompletedTask : EnemyEvents.DispatchEnemySpawnedAsync(context);
    }

    private static async Task DispatchEnemyTurnStartingAsync(ICombatState combatState, IReadOnlyList<Creature> participants)
    {
        foreach (var participant in participants)
        {
            var context = EnemyContext.TryCreate(combatState, participant);
            if (context is not null)
            {
                await EnemyEvents.DispatchEnemyTurnStartingAsync(context);
            }
        }
    }

    private static async Task DispatchEnemyTurnStartedAsync(ICombatState combatState, IReadOnlyList<Creature> participants)
    {
        foreach (var participant in participants)
        {
            var context = EnemyContext.TryCreate(combatState, participant);
            if (context is not null)
            {
                await EnemyEvents.DispatchEnemyTurnStartedAsync(context);
            }
        }
    }

    private static Task DispatchEnemyDyingAsync(IRunState runState, ICombatState? combatState, Creature creature)
    {
        var context = EnemyDyingContext.TryCreate(runState, combatState, creature);
        return context is null ? Task.CompletedTask : EnemyEvents.DispatchEnemyDyingAsync(context);
    }

    private static Task DispatchEnemyDiedAsync(
        IRunState runState,
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        var context = EnemyDiedContext.TryCreate(runState, combatState, creature, wasRemovalPrevented, deathAnimLength);
        return context is null ? Task.CompletedTask : EnemyEvents.DispatchEnemyDiedAsync(context);
    }
}

internal static class HookTaskBridge
{
    internal static async Task After(Task originalTask, Func<Task> continuation)
    {
        await originalTask;
        await continuation();
    }

    internal static void RunDetached(Func<Task> action, string operation)
    {
        _ = RunDetachedAsync(action, operation);
    }

    private static async Task RunDetachedAsync(Func<Task> action, string operation)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"KoraxLib detached lifecycle dispatch failed for {operation}: {ex}");
        }
    }
}
