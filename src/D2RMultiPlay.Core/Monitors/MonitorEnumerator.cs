// ============================================================
// MonitorEnumerator.cs — 枚举系统所有显示器的物理信息
// 调用 Win32 API: EnumDisplayMonitors + GetMonitorInfo + GetDpiForMonitor
// ============================================================

using System.Drawing;
using System.Runtime.InteropServices;
using D2RMultiPlay.Core.Interop;

namespace D2RMultiPlay.Core.Monitors;

public static class MonitorEnumerator
{
    /// <summary>
    /// 枚举当前系统所有显示器，返回完整物理信息列表
    /// </summary>
    public static List<MonitorInfo> Enumerate()
    {
        var monitors = new List<MonitorInfo>();

        // 回调函数：每个显示器调用一次
        NativeMethods.MonitorEnumProc callback = (hMonitor, _, ref WinStructs.RECT _, _) =>
        {
            var info = GetMonitorDetails(hMonitor);
            if (info != null)
                monitors.Add(info);
            return true; // 继续枚举
        };

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        // 按设备名排序，确保顺序稳定
        monitors.Sort((a, b) => string.Compare(a.DeviceName, b.DeviceName, StringComparison.Ordinal));
        return monitors;
    }

    private static MonitorInfo? GetMonitorDetails(IntPtr hMonitor)
    {
        // 获取基本信息（位置、工作区、设备名）
        var mi = new WinStructs.MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf<WinStructs.MONITORINFOEX>();
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            return null;

        // 获取 DPI
        double dpiScale = 1.0;
        int hr = NativeMethods.GetDpiForMonitor(hMonitor, WinConst.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
        if (hr == 0 && dpiX > 0) // S_OK
            dpiScale = dpiX / 96.0;

        // 获取刷新率
        uint refreshHz = 60;
        var dm = new WinStructs.DEVMODE();
        dm.dmSize = (ushort)Marshal.SizeOf<WinStructs.DEVMODE>();
        if (NativeMethods.EnumDisplaySettings(mi.szDevice, WinConst.ENUM_CURRENT_SETTINGS, ref dm))
            refreshHz = dm.dmDisplayFrequency > 0 ? dm.dmDisplayFrequency : 60;

        bool isPrimary = (mi.dwFlags & 0x1) != 0; // MONITORINFOF_PRIMARY

        return new MonitorInfo
        {
            DeviceName = mi.szDevice,
            Bounds = new Rectangle(
                mi.rcMonitor.Left, mi.rcMonitor.Top,
                mi.rcMonitor.Width, mi.rcMonitor.Height),
            WorkArea = new Rectangle(
                mi.rcWork.Left, mi.rcWork.Top,
                mi.rcWork.Width, mi.rcWork.Height),
            DpiScale = dpiScale,
            IsPrimary = isPrimary,
            RefreshHz = refreshHz
        };
    }
}
