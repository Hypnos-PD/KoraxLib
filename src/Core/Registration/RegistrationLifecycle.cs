namespace KoraxLib.Internal.Registration;

/// <summary>
/// KoraxLib 全局内容注册生命周期闸门。
/// </summary>
/// <remarks>
/// STS2 的内容不是任意时刻都能安全注入的：`ModelDb`、遭遇池、序列化缓存等系统
/// 会在初始化期间读取并缓存模型列表。如果某个模组在这些缓存建立后继续注册内容，
/// 不同系统可能看到不同版本的列表，后续错误也很难定位。
///
/// 因此 KoraxLib 采用统一生命周期：初始化早期保持 <see cref="KoraxRegistrationState.Open" />，
/// 到达安全注入点时切到 <see cref="KoraxRegistrationState.Frozen" />。所有内容注册入口都应在
/// 写入自身 registry 前调用 <see cref="ThrowIfFrozen" />，保证 freeze 后不会再出现新的内容声明。
/// </remarks>
internal static class RegistrationLifecycle
{
    private static readonly object SyncRoot = new();
    private static KoraxRegistrationState _state = KoraxRegistrationState.Open;

    /// <summary>
    /// 当前全局注册状态。
    /// </summary>
    public static KoraxRegistrationState State
    {
        get
        {
            lock (SyncRoot)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// 在内容注册入口处检查注册窗口是否仍然开放。
    /// </summary>
    /// <param name="registryName">正在执行注册的 registry 名称，用于生成可定位的错误信息。</param>
    /// <exception cref="ArgumentException"><paramref name="registryName" /> 为空或全空白。</exception>
    /// <exception cref="InvalidOperationException">注册生命周期已经冻结。</exception>
    public static void ThrowIfFrozen(string registryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registryName);

        lock (SyncRoot)
        {
            if (_state == KoraxRegistrationState.Open)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{registryName} can no longer accept content registrations because KoraxLib registration is frozen.");
        }
    }

    /// <summary>
    /// 关闭内容注册窗口。
    /// </summary>
    /// <remarks>
    /// Freeze 是幂等操作。后续可能有多个 patch 点或初始化路径尝试确认注册窗口已关闭，
    /// 重复调用不应该制造额外失败；真正需要失败的是 freeze 之后的新内容注册。
    /// </remarks>
    public static void Freeze()
    {
        lock (SyncRoot)
        {
            _state = KoraxRegistrationState.Frozen;
        }
    }
}
