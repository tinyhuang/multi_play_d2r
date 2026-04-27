// ============================================================
// WindowOps.cs — 窗口操作：查找主窗口、去边框、移动定位
// 完全替代旧脚本中的 NewTitle.exe
// ============================================================

using System.Diagnostics;
using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Interop;

namespace D2RMultiPlay.Core.Windows;

public static class WindowOps
{
    /// <summary>
    /// 根据进程 ID 查找其主窗口句柄
    /// D2R 启动需要一定时间才会创建窗口，因此带超时轮询
    /// </summary>
    /// <param name="processId">目标进程 ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>主窗口句柄，未找到则返回 IntPtr.Zero</returns>
    public static IntPtr FindMainWindow(uint processId, int timeoutMs = 15_000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            IntPtr found = IntPtr.Zero;

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == processId && NativeMethods.IsWindowVisible(hWnd))
                {
                    // 检查是否有标题栏（排除 tooltip 等辅助窗口）
                    var style = (uint)(long)NativeMethods.GetWindowLongPtr(hWnd, WinConst.GWL_STYLE);
                    // D2R 主窗口通常有 WS_CAPTION 或至少是可见的顶层窗口
                    if (style != 0)
                    {
                        found = hWnd;
                        return false; // 停止枚举
                    }
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
                return found;

            Thread.Sleep(500); // 每 500ms 轮询一次
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 移除窗口边框（无边框窗口模式）
    /// </summary>
    public static void RemoveBorder(IntPtr hWnd)
    {
        var style = (uint)(long)NativeMethods.GetWindowLongPtr(hWnd, WinConst.GWL_STYLE);
        style &= ~(WinConst.WS_CAPTION | WinConst.WS_THICKFRAME | WinConst.WS_MINIMIZE
                    | WinConst.WS_MAXIMIZE | WinConst.WS_SYSMENU);
        NativeMethods.SetWindowLongPtr(hWnd, WinConst.GWL_STYLE, (IntPtr)style);

        var exStyle = (uint)(long)NativeMethods.GetWindowLongPtr(hWnd, WinConst.GWL_EXSTYLE);
        exStyle &= ~(WinConst.WS_EX_DLGMODALFRAME | WinConst.WS_EX_CLIENTEDGE | WinConst.WS_EX_STATICEDGE);
        NativeMethods.SetWindowLongPtr(hWnd, WinConst.GWL_EXSTYLE, (IntPtr)exStyle);

        // 通知系统样式已变更
        NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
            WinConst.SWP_FRAMECHANGED | WinConst.SWP_NOZORDER | 0x0001 /*SWP_NOMOVE*/ | 0x0002 /*SWP_NOSIZE*/);
    }

    /// <summary>
    /// 恢复标准窗口边框，便于手动移动和关闭
    /// </summary>
    public static void RestoreBorder(IntPtr hWnd)
    {
        var style = (uint)(long)NativeMethods.GetWindowLongPtr(hWnd, WinConst.GWL_STYLE);
        style |= WinConst.WS_CAPTION | WinConst.WS_THICKFRAME | WinConst.WS_MINIMIZE
                 | WinConst.WS_MAXIMIZE | WinConst.WS_SYSMENU;
        NativeMethods.SetWindowLongPtr(hWnd, WinConst.GWL_STYLE, (IntPtr)style);

        NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
            WinConst.SWP_FRAMECHANGED | WinConst.SWP_NOZORDER | 0x0001 /*SWP_NOMOVE*/ | 0x0002 /*SWP_NOSIZE*/);
    }

    /// <summary>
    /// 将窗口移动到指定位置和大小
    /// </summary>
    public static void MoveWindow(IntPtr hWnd, int x, int y, int w, int h)
    {
        NativeMethods.ShowWindow(hWnd, WinConst.SW_RESTORE); // 确保窗口非最小化
        NativeMethods.SetWindowPos(hWnd, WinConst.HWND_TOP, x, y, w, h,
            WinConst.SWP_NOZORDER | WinConst.SWP_NOOWNERZORDER);
    }

    /// <summary>
    /// 根据布局配置，完成去边框 + 移动定位的完整流程
    /// </summary>
    public static bool ArrangeWindow(uint processId, WindowLayout layout, int findTimeoutMs = 15_000)
    {
        var hWnd = FindMainWindow(processId, findTimeoutMs);
        if (hWnd == IntPtr.Zero)
            return false;

        if (layout.Borderless)
            RemoveBorder(hWnd);
        else
            RestoreBorder(hWnd);

        // 首次落位后再补一轮，覆盖游戏启动后可能的自动居中行为。
        MoveWindow(hWnd, layout.X, layout.Y, layout.W, layout.H);
        Thread.Sleep(700);
        MoveWindow(hWnd, layout.X, layout.Y, layout.W, layout.H);
        return true;
    }
}
