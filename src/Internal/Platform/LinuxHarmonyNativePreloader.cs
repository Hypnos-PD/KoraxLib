using System.Runtime.InteropServices;

namespace KoraxLib.Internal.Platform;

/// <summary>
/// 在 Linux 上为 Harmony/MonoMod 的 native helper 预加载 unwind 相关库。
/// </summary>
/// <remarks>
/// STS2 的 Linux 环境里，Harmony 动态 patch 可能会生成临时 native helper。
/// 如果 <c>libgcc_s</c> / <c>libunwind</c> 没有以 <c>RTLD_GLOBAL</c> 方式加载，helper 可能因为
/// 找不到 <c>_Unwind_RaiseException</c> 而初始化失败。KoraxLib 是前置库，不能依赖其它 mod
/// 先替我们完成这一步。
/// </remarks>
internal static class LinuxHarmonyNativePreloader
{
    private const int RtldNow = 2;
    private const int RtldGlobal = 0x100;
    private const int DlopenFlags = RtldNow | RtldGlobal;

    private static readonly object SyncRoot = new();
    private static readonly List<IntPtr> Handles = [];
    private static bool _attempted;

    private static readonly string[] CandidateLibraries =
    [
        "libgcc_s.so.1",
        "libunwind.so.8",
        "libunwind.so",
    ];

    internal static void EnsureLoaded(Action<string> info, Action<string> warn)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(warn);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_attempted)
            {
                return;
            }

            _attempted = true;
            TryLoad(info, warn);
        }
    }

#pragma warning disable CA2101
    [DllImport("libdl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
    private static extern IntPtr Dlopen(string filename, int flags);

    [DllImport("libdl.so.2", EntryPoint = "dlerror")]
    private static extern IntPtr Dlerror();
#pragma warning restore CA2101

    private static void TryLoad(Action<string> info, Action<string> warn)
    {
        var loaded = new List<string>();
        var failures = new List<string>();

        try
        {
            foreach (var library in CandidateLibraries)
            {
                var handle = Dlopen(library, DlopenFlags);
                if (handle != IntPtr.Zero)
                {
                    Handles.Add(handle);
                    loaded.Add(library);
                    continue;
                }

                failures.Add($"{library}: {GetLastDlError()}");
            }
        }
        catch (DllNotFoundException ex)
        {
            warn($"Linux Harmony native preload skipped: libdl.so.2 is unavailable ({ex.Message}).");
            return;
        }
        catch (EntryPointNotFoundException ex)
        {
            warn($"Linux Harmony native preload skipped: libdl.so.2 is missing dlopen/dlerror ({ex.Message}).");
            return;
        }

        if (loaded.Count > 0)
        {
            info($"Linux Harmony native preload loaded with RTLD_GLOBAL: {string.Join(", ", loaded)}.");
            return;
        }

        warn(
            "Linux Harmony native preload could not load libgcc/libunwind candidates; Harmony may still work if the host already exports unwind symbols. "
            + $"Tried: {string.Join("; ", failures)}");
    }

    private static string GetLastDlError()
    {
        var error = Dlerror();
        return error == IntPtr.Zero
            ? "unknown dlopen error"
            : Marshal.PtrToStringAnsi(error) ?? "unknown dlopen error";
    }
}
