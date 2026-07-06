using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Models.Acts;

namespace KoraxLib.Internal.Smoke;

/// <summary>
/// 只用于手动验证 KoraxLib 内容注册和 act encounter 合并 patch 的内部 smoke bootstrap。
/// </summary>
internal static class SmokeTestContent
{
    private const string EnableEnvironmentVariable = "KORAXLIB_ENABLE_SMOKE_CONTENT";

    /// <summary>
    /// 当环境变量 <c>KORAXLIB_ENABLE_SMOKE_CONTENT=1</c> 时，把 smoke encounter 注册进 Overgrowth。
    /// </summary>
    /// <remarks>
    /// 默认关闭，避免 KoraxLib 作为前置库时向正常游戏流程注入测试遭遇。
    /// </remarks>
    internal static void RegisterIfEnabled()
    {
        if (Environment.GetEnvironmentVariable(EnableEnvironmentVariable) != "1")
        {
            return;
        }

        EnemyRegistry.RegisterActEncounter<Overgrowth, KoraxSmokeEncounter>();
        Entry.Logger.Info("KoraxLib smoke encounter registration enabled for Overgrowth.");
    }
}
