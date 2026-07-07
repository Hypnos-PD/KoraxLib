using KoraxLib.Enemies;
using KoraxLib.Enemies.PowerTransfer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace KoraxLib.Internal.Smoke;

/// <summary>
/// 通过环境变量开启的 PowerTransfer smoke，用于游戏内验证 SafeClone 转移链路。
/// </summary>
internal static class PowerTransferSmokeRunner
{
    private const string EnableEnvironmentVariable = "KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE";
    private const int SourceAmount = 3;

    private static readonly object SyncRoot = new();
    private static bool _enabled;
    private static bool _completed;

    /// <summary>
    /// 当 <c>KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1</c> 时订阅敌人生成事件。
    /// </summary>
    internal static void EnableIfRequested()
    {
        if (Environment.GetEnvironmentVariable(EnableEnvironmentVariable) != "1")
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_enabled)
            {
                return;
            }

            EnemyEvents.EnemySpawned += RunOnSmokeEnemySpawnedAsync;
            _enabled = true;
        }

        Entry.Logger.Info("KoraxLib PowerTransfer smoke runner enabled.");
    }

    private static async Task RunOnSmokeEnemySpawnedAsync(EnemyContext context)
    {
        if (_completed || context.Monster is not Nibbit)
        {
            return;
        }

        var player = context.CombatState.PlayerCreatures.FirstOrDefault();
        if (player is null)
        {
            Entry.Logger.Warn("[PowerTransferSmoke] Skipped: no player creature found.");
            return;
        }

        var sourcePower = await PowerCmd.Apply<PlatingPower>(
            new BlockingPlayerChoiceContext(),
            context.Creature,
            SourceAmount,
            context.Creature,
            null,
            silent: true);

        if (sourcePower is null)
        {
            Entry.Logger.Warn("[PowerTransferSmoke] Failed: could not apply source PlatingPower to enemy.");
            return;
        }

        var result = await PowerTransferService.TransferAsync(new PowerTransferRequest
        {
            ChoiceContext = new BlockingPlayerChoiceContext(),
            SourcePower = sourcePower,
            Target = player,
            Applier = player,
            RemoveSource = true,
            Silent = true,
        });

        var enemyHasSource = context.Creature.GetPower<PlatingPower>() is not null;
        var playerPower = player.GetPower<PlatingPower>();
        var playerAmount = playerPower?.Amount ?? 0;

        Entry.Logger.Info(
            "[PowerTransferSmoke] Result: " +
            $"Status={result.Status}, Safety={result.Safety}, SourceRemoved={result.SourceRemoved}, " +
            $"EnemyHasSource={enemyHasSource}, PlayerPlating={playerAmount}");

        _completed = true;
    }
}
