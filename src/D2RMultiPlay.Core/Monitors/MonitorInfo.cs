// ============================================================
// MonitorInfo.cs — 单个显示器的运行时信息
// ============================================================

namespace D2RMultiPlay.Core.Monitors;

/// <summary>
/// 表示一个物理显示器的完整信息（运行时采集结果）
/// </summary>
public sealed class MonitorInfo
{
    /// <summary>设备名，如 \\.\DISPLAY1（用作稳定标识）</summary>
    public string DeviceName { get; init; } = "";

    /// <summary>显示器完整边界（含任务栏区域）</summary>
    public System.Drawing.Rectangle Bounds { get; init; }

    /// <summary>工作区（排除任务栏）</summary>
    public System.Drawing.Rectangle WorkArea { get; init; }

    /// <summary>DPI 缩放比（1.0 = 100%, 1.5 = 150%）</summary>
    public double DpiScale { get; init; } = 1.0;

    /// <summary>是否为主显示器</summary>
    public bool IsPrimary { get; init; }

    /// <summary>当前刷新率 (Hz)</summary>
    public uint RefreshHz { get; init; } = 60;

    public override string ToString() =>
        $"{DeviceName} {Bounds.Width}x{Bounds.Height} @{DpiScale * 100:F0}% {RefreshHz}Hz{(IsPrimary ? " [Primary]" : "")}";
}
