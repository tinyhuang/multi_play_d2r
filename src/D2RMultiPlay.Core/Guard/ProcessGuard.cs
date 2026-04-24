// ============================================================
// ProcessGuard.cs — 进程守卫：监控窗口存活状态 + 快速重连
// 使用 Timer 定期巡检，通过事件通知 UI 层
// ============================================================

using System.Diagnostics;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.Core.Guard;

/// <summary>
/// 单个被监控实例的运行时状态
/// </summary>
public sealed class InstanceState
{
    public int AccountId { get; init; }
    public uint ProcessId { get; set; }
    public IntPtr ProcessHandle { get; set; }
    public IntPtr WindowHandle { get; set; }
    public bool IsAlive { get; set; }
    public DateTime LaunchedAt { get; set; }
    public DateTime? DiedAt { get; set; }
}

/// <summary>
/// 进程状态变化事件参数
/// </summary>
public sealed class InstanceStateChangedEventArgs : EventArgs
{
    public required InstanceState State { get; init; }
    public bool WasAlive { get; init; }
}

public sealed class ProcessGuard : IDisposable
{
    private readonly Dictionary<int, InstanceState> _instances = new();
    private readonly System.Threading.Timer _timer;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>巡检间隔（毫秒），默认 3 秒</summary>
    public int IntervalMs { get; set; } = 3000;

    /// <summary>当某个实例状态发生变化时触发</summary>
    public event EventHandler<InstanceStateChangedEventArgs>? StateChanged;

    public ProcessGuard()
    {
        _timer = new System.Threading.Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>开始巡检</summary>
    public void Start()
    {
        _timer.Change(IntervalMs, IntervalMs);
    }

    /// <summary>停止巡检</summary>
    public void Stop()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 注册一个已启动的实例到监控列表
    /// </summary>
    public void Register(int accountId, uint processId, IntPtr processHandle)
    {
        lock (_lock)
        {
            _instances[accountId] = new InstanceState
            {
                AccountId = accountId,
                ProcessId = processId,
                ProcessHandle = processHandle,
                IsAlive = true,
                LaunchedAt = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 从监控列表移除一个实例
    /// </summary>
    public void Unregister(int accountId)
    {
        lock (_lock)
        {
            _instances.Remove(accountId);
        }
    }

    /// <summary>
    /// 获取所有被监控实例的状态快照
    /// </summary>
    public List<InstanceState> GetAllStates()
    {
        lock (_lock)
        {
            return [.. _instances.Values];
        }
    }

    /// <summary>
    /// 获取指定账号的状态
    /// </summary>
    public InstanceState? GetState(int accountId)
    {
        lock (_lock)
        {
            return _instances.GetValueOrDefault(accountId);
        }
    }

    // ---- 定时巡检回调 ----

    private void OnTick(object? _)
    {
        List<InstanceState> snapshot;
        lock (_lock)
        {
            snapshot = [.. _instances.Values];
        }

        foreach (var state in snapshot)
        {
            bool wasAlive = state.IsAlive;
            bool nowAlive = IsProcessAlive(state.ProcessId);

            if (wasAlive != nowAlive)
            {
                state.IsAlive = nowAlive;
                if (!nowAlive)
                    state.DiedAt = DateTime.Now;

                StateChanged?.Invoke(this, new InstanceStateChangedEventArgs
                {
                    State = state,
                    WasAlive = wasAlive
                });
            }
        }
    }

    private static bool IsProcessAlive(uint processId)
    {
        try
        {
            var proc = Process.GetProcessById((int)processId);
            return !proc.HasExited;
        }
        catch
        {
            return false; // 进程已不存在
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
    }
}
