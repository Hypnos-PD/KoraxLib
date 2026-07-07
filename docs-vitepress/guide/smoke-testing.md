# Smoke Testing

KoraxLib includes opt-in smoke content for local runtime verification. It is disabled by default.

## Linux Launcher

Use the helper script from the repository root:

```bash
scripts/run-smoke-linux.sh
```

The script reads `.env` by default. Start from `.env.example-linux` and set your local STS2 path if needed.

## Environment Flags

```bash
KORAXLIB_ENABLE_SMOKE_CONTENT=1
KORAXLIB_ENABLE_LIFECYCLE_SMOKE=1
KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1
```

- `KORAXLIB_ENABLE_SMOKE_CONTENT=1` registers `KoraxSmokeEncounter` in `Overgrowth`.
- `KORAXLIB_ENABLE_LIFECYCLE_SMOKE=1` enables lifecycle event logging.
- `KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1` enables the PowerTransfer SafeClone smoke runner.

## Trigger The Encounter

Use the STS2 developer console:

```text
fight KORAX_SMOKE_ENCOUNTER
```

Expected combat creation log:

```text
Creating NCombatRoom with mode=ActiveCombat encounter=KORAX_SMOKE_ENCOUNTER.
```

## Expected Lifecycle Logs

```text
[KoraxLib] [LifecycleSmoke] EnemySpawned: MonsterType=Nibbit, CombatId=1, CurrentHp=46
[KoraxLib] [LifecycleSmoke] EnemyDying: MonsterType=Nibbit, CombatId=1, CurrentHp=0
[KoraxLib] [LifecycleSmoke] EnemyDied: MonsterType=Nibbit, CombatId=1, CurrentHp=0, WasRemovalPrevented=False, DeathAnimationDurationSeconds=0.86666673
```

If the enemy survives to its turn, turn-starting and turn-started logs should also appear.

## Expected PowerTransfer Logs

When `KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1` is enabled, the smoke runner waits for the smoke `Nibbit` to spawn, applies `PlatingPower` to it, then transfers that power to the first player creature through `PowerTransferService.TransferAsync`.

Expected result shape:

```text
[KoraxLib] [PowerTransferSmoke] Result: Status=Applied, Safety=SafeClone, SourceRemoved=True, EnemyHasSource=False, PlayerPlating=3
```

This verifies the safe-clone path at runtime:

1. The source enemy can receive a known safe-clone buff.
2. `PowerTransferService.TransferAsync` can clone and apply that buff to the player.
3. The source buff is removed when `RemoveSource=true`.
4. Combat continues after the transfer command chain.

## Save Serialization Check

After winning or saving the run, confirm that logs contain save writes and do not contain:

```text
Unknown ModelId entry 'ENCOUNTER.KORAX_SMOKE_ENCOUNTER'
```

The latest manual smoke run verified encounter victory, progress save writes, and no unknown model ID serialization error.

## Known Unrelated Noise

During a modded STS2 session you may see unrelated messages from other mods or from Godot renderer shutdown, such as missing STSVWB audio folders or RID/texture leaks at exit. Those do not indicate KoraxLib lifecycle or serialization failure unless they include KoraxLib-specific errors.
