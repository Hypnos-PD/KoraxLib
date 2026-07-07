# Smoke 测试

KoraxLib 包含 opt-in smoke content，用于本地运行时验证。默认不开启。

## Linux 启动脚本

在仓库根目录运行：

```bash
scripts/run-smoke-linux.sh
```

脚本默认读取 `.env`。可以从 `.env.example-linux` 开始，根据本地 STS2 路径调整。

## 环境变量

```bash
KORAXLIB_ENABLE_SMOKE_CONTENT=1
KORAXLIB_ENABLE_LIFECYCLE_SMOKE=1
KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1
```

- `KORAXLIB_ENABLE_SMOKE_CONTENT=1` 会把 `KoraxSmokeEncounter` 注册到 `Overgrowth`。
- `KORAXLIB_ENABLE_LIFECYCLE_SMOKE=1` 会启用 lifecycle event 日志。
- `KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1` 会启用 PowerTransfer SafeClone smoke runner。

## 触发 Encounter

使用 STS2 developer console：

```text
fight KORAX_SMOKE_ENCOUNTER
```

预期 combat 创建日志：

```text
Creating NCombatRoom with mode=ActiveCombat encounter=KORAX_SMOKE_ENCOUNTER.
```

## 预期生命周期日志

```text
[KoraxLib] [LifecycleSmoke] EnemySpawned: MonsterType=Nibbit, CombatId=1, CurrentHp=46
[KoraxLib] [LifecycleSmoke] EnemyDying: MonsterType=Nibbit, CombatId=1, CurrentHp=0
[KoraxLib] [LifecycleSmoke] EnemyDied: MonsterType=Nibbit, CombatId=1, CurrentHp=0, WasRemovalPrevented=False, DeathAnimationDurationSeconds=0.86666673
```

如果敌人活到敌方回合，还应看到 turn-starting 和 turn-started 相关日志。

## 预期 PowerTransfer 日志

启用 `KORAXLIB_ENABLE_POWER_TRANSFER_SMOKE=1` 后，smoke runner 会等待 smoke `Nibbit` 生成，先给它施加 `PlatingPower`，再通过 `PowerTransferService.TransferAsync` 把这个 power 转移给第一个玩家 creature。

预期结果形态：

```text
[KoraxLib] [PowerTransferSmoke] Result: Status=Applied, Safety=SafeClone, SourceRemoved=True, EnemyHasSource=False, PlayerPlating=3
```

这会在运行时验证 safe-clone 路径：

1. 源敌人可以获得一个已知可 safe-clone 的 Buff。
2. `PowerTransferService.TransferAsync` 可以克隆并应用该 Buff 到玩家。
3. `RemoveSource=true` 时源 Buff 会被移除。
4. 转移命令链执行后战斗继续。

## 存档序列化检查

胜利或保存 run 后，确认日志有 save writes，并且没有：

```text
Unknown ModelId entry 'ENCOUNTER.KORAX_SMOKE_ENCOUNTER'
```

最新手动 smoke 已验证 encounter victory、progress save writes，并且没有 unknown model ID serialization error。

## 已知无关噪音

Modded STS2 session 中可能出现来自其它 mod 或 Godot renderer 退出清理的日志，例如 STSVWB audio folder 缺失、退出时 RID/texture leak。这些不表示 KoraxLib lifecycle 或 serialization 失败，除非日志中出现 KoraxLib 相关错误。
