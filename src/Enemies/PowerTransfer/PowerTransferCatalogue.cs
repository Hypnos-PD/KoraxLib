using System.Collections.ObjectModel;
using KoraxLib.Internal.Registration;
using MegaCrit.Sts2.Core.Models;

namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 原版 enemy power 转移兼容性目录。
/// </summary>
/// <remarks>
/// 初始数据来自 STSVWB <c>AchimPowerTransferMapper</c> 对“绝望之王·阿基姆”偷取敌方 Buff 的兼容性研究。
/// 未知 power 默认视为 <see cref="PowerTransferSafety.Unsupported" />，避免把依赖敌人生命周期的能力错误转移到玩家或其他 creature 上。
/// </remarks>
public static class PowerTransferCatalogue
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, PowerTransferEntry> EntriesByClassName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, PowerTransferEntry> EntriesByPowerId = new(StringComparer.OrdinalIgnoreCase);

    static PowerTransferCatalogue()
    {
        SeedDefaults();
    }

    /// <summary>
    /// 已知兼容性条目的防御性快照。
    /// </summary>
    public static IReadOnlyCollection<PowerTransferEntry> All
    {
        get
        {
            lock (SyncRoot)
            {
                return new ReadOnlyCollection<PowerTransferEntry>(EntriesByClassName.Values.ToArray());
            }
        }
    }

    /// <summary>
    /// 注册一个新的 power 转移兼容性条目。
    /// </summary>
    /// <exception cref="InvalidOperationException">KoraxLib 注册窗口已经冻结。</exception>
    public static void Register(PowerTransferEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        RegistrationLifecycle.ThrowIfFrozen(nameof(PowerTransferCatalogue));
        Validate(entry);

        lock (SyncRoot)
        {
            if (!EntriesByClassName.TryAdd(entry.PowerClassName, entry))
            {
                Entry.Logger.Debug($"PowerTransferCatalogue ignored duplicate class entry: {entry.PowerClassName}.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(entry.PowerId))
            {
                EntriesByPowerId.TryAdd(entry.PowerId, entry);
            }
        }
    }

    /// <summary>
    /// 查询指定 power 模型实例的转移安全分类。
    /// </summary>
    public static PowerTransferSafety Classify(PowerModel power)
    {
        return TryGetEntry(power, out var entry) ? entry.Safety : PowerTransferSafety.Unsupported;
    }

    /// <summary>
    /// 查询指定 power 类型的转移安全分类。
    /// </summary>
    public static PowerTransferSafety Classify(Type powerType)
    {
        return TryGetEntry(powerType, out var entry) ? entry.Safety : PowerTransferSafety.Unsupported;
    }

    /// <summary>
    /// 查询指定 power 类型名的转移安全分类。
    /// </summary>
    public static PowerTransferSafety Classify(string powerClassName)
    {
        return TryGetEntry(powerClassName, out var entry) ? entry.Safety : PowerTransferSafety.Unsupported;
    }

    /// <summary>
    /// 尝试获取指定 power 模型实例的兼容性条目。
    /// </summary>
    public static bool TryGetEntry(PowerModel power, out PowerTransferEntry entry)
    {
        ArgumentNullException.ThrowIfNull(power);

        lock (SyncRoot)
        {
            if (EntriesByPowerId.TryGetValue(power.Id.Entry, out entry!))
            {
                return true;
            }

            return EntriesByClassName.TryGetValue(power.GetType().Name, out entry!);
        }
    }

    /// <summary>
    /// 尝试获取指定 power 类型的兼容性条目。
    /// </summary>
    public static bool TryGetEntry(Type powerType, out PowerTransferEntry entry)
    {
        ArgumentNullException.ThrowIfNull(powerType);
        return TryGetEntry(powerType.Name, out entry);
    }

    /// <summary>
    /// 尝试获取指定 power 类型名的兼容性条目。
    /// </summary>
    public static bool TryGetEntry(string powerClassName, out PowerTransferEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(powerClassName);

        lock (SyncRoot)
        {
            return EntriesByClassName.TryGetValue(NormalizePowerClassName(powerClassName), out entry!);
        }
    }

    private static void SeedDefaults()
    {
        foreach (var powerClassName in SafeClonePowerClassNames)
        {
            AddSeed(PowerTransferEntry.SafeClone(powerClassName));
        }

        AddSeed(PowerTransferEntry.NeedsAdapter("CurlUpPower", "CURL_UP_POWER", "CURL_UP_POWER") with
        {
            Notes = "原版实现会写入 LouseProgenitor.Curled，转移时需要替代实现。",
        });
        AddSeed(PowerTransferEntry.NeedsAdapter("SkittishPower", "SKITTISH_POWER", "SKITTISH_POWER") with
        {
            Notes = "原版实现依赖怪物受击动画和每回合状态，转移时需要替代实现。",
        });
        AddSeed(PowerTransferEntry.NeedsAdapter("StockPower", "STOCK_POWER") with
        {
            Notes = "原版库存逻辑与敌方死亡替换相关，玩家侧应使用计数型替代实现。",
        });

        foreach (var powerClassName in UnsupportedPowerClassNames)
        {
            AddSeed(PowerTransferEntry.Unsupported(powerClassName));
        }

        foreach (var powerId in UnsupportedPowerIds)
        {
            AddSeed(PowerTransferEntry.Unsupported(powerId, powerId));
        }
    }

    private static void AddSeed(PowerTransferEntry entry)
    {
        Validate(entry);
        EntriesByClassName[entry.PowerClassName] = entry;
        if (!string.IsNullOrWhiteSpace(entry.PowerId))
        {
            EntriesByPowerId[entry.PowerId] = entry;
        }
    }

    private static void Validate(PowerTransferEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.PowerClassName);
        if (entry.Safety == PowerTransferSafety.NeedsAdapter)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.AdapterKey);
        }
    }

    private static string NormalizePowerClassName(string powerClassName)
    {
        var separator = powerClassName.LastIndexOf('.');
        return separator < 0 ? powerClassName : powerClassName[(separator + 1)..];
    }

    private static readonly string[] SafeClonePowerClassNames =
    [
        "AfterimagePower",
        "BarricadePower",
        "DarkEmbracePower",
        "DexterityPower",
        "FeralPower",
        "FlutterPower",
        "GravityPower",
        "HardenedShellPower",
        "InfestedPower",
        "OrbitPower",
        "PanachePower",
        "PlatingPower",
        "RampartPower",
        "RavenousPower",
        "RupturePower",
        "SerpentFormPower",
        "StormPower",
        "UnmovablePower",
        "VigorPower",
    ];

    private static readonly string[] UnsupportedPowerClassNames =
    [
        "AdaptablePower",
        "AsleepPower",
        "AutomationPower",
        "CalamityPower",
        "ColossusPower",
        "CoveredPower",
        "DiamondDiademPower",
        "FastenPower",
        "GigantificationPower",
        "HatchPower",
        "IllusionPower",
        "InterceptPower",
        "JugglingPower",
        "MockRevivePower",
        "MonologuePower",
        "NecroMasteryPower",
        "NightmarePower",
        "OutbreakPower",
        "PersonalHivePower",
        "PossessSpeedPower",
        "PossessStrengthPower",
        "ReattachPower",
        "SandpitPower",
        "SlumberPower",
        "SteamEruptionPower",
        "SubroutinePower",
        "SurprisePower",
        "SwipePower",
        "ThieveryPower",
        "VitalSparkPower",
        "VoidFormPower",
    ];

    private static readonly string[] UnsupportedPowerIds =
    [
        "COVERED_POWER",
        "HATCH_POWER",
        "INTERCEPT_POWER",
        "SWIPE_POWER",
        "THIEVERY_POWER",
    ];
}
