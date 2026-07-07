using System.Collections.ObjectModel;
using KoraxLib.Internal.Registration;

namespace KoraxLib.Enemies.PowerTransfer;

/// <summary>
/// 管理 <see cref="IPowerTransferAdapter" /> 实例的注册表。
/// </summary>
public static class PowerTransferAdapterRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, IPowerTransferAdapter> AdaptersByClassName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 当前已注册适配器的防御性快照。
    /// </summary>
    public static IReadOnlyCollection<IPowerTransferAdapter> RegisteredAdapters
    {
        get
        {
            lock (SyncRoot)
            {
                return new ReadOnlyCollection<IPowerTransferAdapter>(AdaptersByClassName.Values.ToArray());
            }
        }
    }

    /// <summary>
    /// 注册一个 power 转移适配器。
    /// </summary>
    /// <exception cref="InvalidOperationException">KoraxLib 注册窗口已经冻结。</exception>
    public static void Register(IPowerTransferAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapter.AdaptedPowerClassName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapter.ReplacementPowerClassName);
        RegistrationLifecycle.ThrowIfFrozen(nameof(PowerTransferAdapterRegistry));

        lock (SyncRoot)
        {
            if (!AdaptersByClassName.TryAdd(NormalizePowerClassName(adapter.AdaptedPowerClassName), adapter))
            {
                Entry.Logger.Debug(
                    $"PowerTransferAdapterRegistry ignored duplicate adapter registration: {adapter.AdaptedPowerClassName}.");
            }
        }
    }

    /// <summary>
    /// 尝试查找处理指定原版 power 类型名的适配器。
    /// </summary>
    public static bool TryGet(string powerClassName, out IPowerTransferAdapter adapter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(powerClassName);

        lock (SyncRoot)
        {
            return AdaptersByClassName.TryGetValue(NormalizePowerClassName(powerClassName), out adapter!);
        }
    }

    private static string NormalizePowerClassName(string powerClassName)
    {
        var separator = powerClassName.LastIndexOf('.');
        return separator < 0 ? powerClassName : powerClassName[(separator + 1)..];
    }
}
