"""Auto-detect monitor configuration on Windows via ctypes."""

import sys


def detect_monitors():
    """Detect monitors and return a list of dicts.

    Each dict has keys: W, H, SCALE, X, Y, TASKBAR.
    Returns None if not on Windows.
    Returns empty list if detection fails.
    """
    if sys.platform != "win32":
        return None

    try:
        return _detect_windows()
    except Exception:
        return []


def _detect_windows():
    import ctypes
    from ctypes import wintypes, Structure, POINTER, byref

    user32 = ctypes.windll.user32

    # Make process per-monitor DPI-aware so we get real physical coordinates
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)  # PROCESS_PER_MONITOR_DPI_AWARE
    except Exception:
        try:
            user32.SetProcessDPIAware()
        except Exception:
            pass

    class MONITORINFOEXW(Structure):
        _fields_ = [
            ("cbSize", wintypes.DWORD),
            ("rcMonitor", wintypes.RECT),
            ("rcWork", wintypes.RECT),
            ("dwFlags", wintypes.DWORD),
            ("szDevice", wintypes.WCHAR * 32),
        ]

    monitors = []

    def _callback(hMonitor, hdcMonitor, lprcMonitor, dwData):
        info = MONITORINFOEXW()
        info.cbSize = ctypes.sizeof(MONITORINFOEXW)
        if not user32.GetMonitorInfoW(hMonitor, byref(info)):
            return 1

        rc = info.rcMonitor
        phys_w = rc.right - rc.left
        phys_h = rc.bottom - rc.top

        # Get effective DPI for this monitor
        scale = 100
        try:
            shcore = ctypes.windll.shcore
            dpiX = ctypes.c_uint()
            dpiY = ctypes.c_uint()
            # MDT_EFFECTIVE_DPI = 0
            shcore.GetDpiForMonitor(hMonitor, 0, byref(dpiX), byref(dpiY))
            scale = round(dpiX.value / 96 * 100)
        except Exception:
            pass

        # Taskbar height: difference between monitor rect and work rect
        taskbar = 0
        work_h = info.rcWork.bottom - info.rcWork.top
        mon_h = phys_h
        if work_h < mon_h:
            taskbar = mon_h - work_h

        monitors.append({
            "W": phys_w,
            "H": phys_h,
            "SCALE": scale,
            "X": rc.left,
            "Y": rc.top,
            "TASKBAR": taskbar,
        })
        return 1

    MONITORENUMPROC = ctypes.WINFUNCTYPE(
        ctypes.c_int,
        wintypes.HMONITOR, wintypes.HDC,
        POINTER(wintypes.RECT), wintypes.LPARAM,
    )
    enum_proc = MONITORENUMPROC(_callback)
    user32.EnumDisplayMonitors(None, None, enum_proc, 0)

    # Sort by X then Y so numbering is left-to-right, top-to-bottom
    monitors.sort(key=lambda m: (m["X"], m["Y"]))

    return monitors
