using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Sts2Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace KoraxLib;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    private static readonly object SyncRoot = new();
    private static Harmony? _harmony;

    /// <summary>
    /// KoraxLib 持有的 Harmony 实例，供 Internal.Patching 层进行动态 patch 安装。
    /// </summary>
    internal static Harmony Harmony => _harmony
        ?? throw new InvalidOperationException("KoraxLib Harmony instance is not initialized.");

    public static Sts2Logger Logger { get; } = CreateLogger(Const.ModId);
    public static bool IsInitialized { get; private set; }

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (IsInitialized)
            {
                Logger.Debug("KoraxLib already initialized, skipping duplicate initialization.");
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            _harmony ??= new Harmony($"{Const.ModId}.harmony");
            _harmony.PatchAll(assembly);
            EnsureGodotScriptsRegistered(assembly);

            IsInitialized = true;
            Logger.Info($"{Const.Name} v{Const.Version} initialized.");
        }
    }

    public static Sts2Logger CreateLogger(string modId, LogType logType = LogType.Generic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        return new Sts2Logger(modId, logType);
    }

    private static void EnsureGodotScriptsRegistered(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var bridgeType = typeof(GodotObject).Assembly.GetType("Godot.Bridge.ScriptManagerBridge");
        var lookupMethod = bridgeType?.GetMethod(
            "LookupScriptsInAssembly",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(Assembly)],
            null);

        lookupMethod?.Invoke(null, [assembly]);
    }
}
