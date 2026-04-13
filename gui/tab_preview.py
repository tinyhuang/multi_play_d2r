"""Layout preview tab with drag-and-drop window positioning."""

import math
import tkinter as tk
from tkinter import ttk

# Colors
_MON_BG = "#e0e0e0"
_MON_BORDER = "#888888"
_PLAY_COLOR = "#4a90d9"
_PLAY_BORDER = "#2a5fa0"
_FILL_COLOR = "#b0b0b0"
_FILL_BORDER = "#707070"
_DRAG_COLOR = "#ff9900"
_TEXT_COLOR = "#ffffff"
_MON_LABEL_COLOR = "#555555"


class PreviewTab:
    def __init__(self, parent, i18n):
        self.i18n = i18n
        self.frame = ttk.Frame(parent, padding=8)
        self._custom_positions = {}  # {account_id_str: {x, y}}
        self._windows = []  # list of window info dicts
        self._monitors = {}
        self._drag_data = None
        self._canvas_items = {}  # account_id -> (rect_id, text_id)

        self._build()

    def _build(self):
        # Toolbar
        bar = ttk.Frame(self.frame)
        bar.pack(fill=tk.X, pady=(0, 4))

        self._btn_auto = ttk.Button(
            bar, text=self.i18n.t("auto_layout"), command=self._auto_layout,
        )
        self._btn_auto.pack(side=tk.LEFT, padx=(0, 8))

        self._btn_reset = ttk.Button(
            bar, text=self.i18n.t("reset_positions"), command=self._reset_positions,
        )
        self._btn_reset.pack(side=tk.LEFT)

        self._info_var = tk.StringVar(value="")
        ttk.Label(bar, textvariable=self._info_var).pack(side=tk.RIGHT, padx=8)

        # Canvas
        self.canvas = tk.Canvas(
            self.frame, bg="#f5f5f5", highlightthickness=1,
            highlightbackground="#cccccc",
        )
        self.canvas.pack(fill=tk.BOTH, expand=True)

        self.canvas.bind("<Configure>", lambda e: self._redraw())

    def update_layout(self, monitors, accounts, global_cfg):
        """Called when switching to preview tab. Recalculates everything."""
        self._monitors = monitors
        self._global_cfg = global_cfg
        self._accounts = [a for a in accounts if a.get("ENABLE") == "1"]

        # Auto-assign monitors to unassigned accounts
        self._assign_monitors()

        # Calculate initial positions (auto layout) for accounts without custom pos
        self._calc_auto_positions()

        self._redraw()

    def _assign_monitors(self):
        """Assign monitor to accounts that have no MONITOR set."""
        mon_ids = sorted(self._monitors.keys(), key=lambda x: int(x))
        if not mon_ids:
            return

        # Count per monitor
        counts = {mid: 0 for mid in mon_ids}
        for acct in self._accounts:
            mid = acct.get("MONITOR", "").strip()
            if mid and mid in counts:
                counts[mid] += 1

        for acct in self._accounts:
            mid = acct.get("MONITOR", "").strip()
            if not mid or mid not in counts:
                # Assign to least loaded
                best = min(counts, key=counts.get)
                acct["_assigned_monitor"] = best
                counts[best] += 1
            else:
                acct["_assigned_monitor"] = mid

    def _calc_auto_positions(self):
        """Calculate window positions using the same grid algorithm as the bat."""
        default_w = int(self._global_cfg.get("DEFAULT_WIN_W", "1280") or "1280")
        default_h = int(self._global_cfg.get("DEFAULT_WIN_H", "720") or "720")
        primary_w = int(self._global_cfg.get("PRIMARY_WIN_W", "1920") or "1920")
        primary_h = int(self._global_cfg.get("PRIMARY_WIN_H", "1080") or "1080")
        min_w = int(self._global_cfg.get("MIN_WIN_W", "800") or "800")
        min_h = int(self._global_cfg.get("MIN_WIN_H", "600") or "600")

        # Group accounts by monitor
        per_mon = {}
        for acct in self._accounts:
            mid = acct.get("_assigned_monitor", "1")
            per_mon.setdefault(mid, []).append(acct)

        self._windows = []

        for mid, accts in per_mon.items():
            mon = self._monitors.get(mid, {"W": 1920, "H": 1080, "SCALE": 100, "X": 0, "Y": 0, "TASKBAR": 0})
            scale = max(mon.get("SCALE", 100), 1)
            log_w = mon["W"] * 100 // scale
            log_h = mon["H"] * 100 // scale - mon.get("TASKBAR", 0)
            if log_h < 600:
                log_h = 600
            mon_x = mon.get("X", 0)
            mon_y = mon.get("Y", 0)

            n = len(accts)
            cols, rows = self._calc_grid(n, log_w, log_h, min_w)

            # Cell size and window size
            cell_w = log_w // max(cols, 1)
            cell_h = log_h // max(rows, 1)
            win_w = min(default_w, cell_w)
            win_h = min(default_h, cell_h)
            if win_w < min_w:
                win_w = min_w
            if win_h < min_h:
                win_h = min_h

            # Step calculation
            safe_x = max(log_w - win_w, 0)
            safe_y = max(log_h - win_h, 0)
            step_x = win_w if cols <= 1 else min(safe_x // (cols - 1), win_w)
            step_y = win_h if rows <= 1 else min(safe_y // (rows - 1), win_h)

            for idx, acct in enumerate(accts):
                aid = str(acct["id"])
                is_primary = acct.get("PRIMARY", "0").strip()[:1] == "1"

                # Check for drag-customized position
                if aid in self._custom_positions:
                    px = self._custom_positions[aid]["x"]
                    py = self._custom_positions[aid]["y"]
                else:
                    col = idx % cols
                    row = idx // cols
                    px = mon_x + col * step_x
                    py = mon_y + row * step_y
                    # Bounds check
                    px = min(px, mon_x + safe_x)
                    py = min(py, mon_y + safe_y)

                w = primary_w if is_primary else win_w
                h = primary_h if is_primary else win_h

                self._windows.append({
                    "id": aid,
                    "x": px, "y": py, "w": w, "h": h,
                    "primary": is_primary,
                    "monitor": mid,
                    "mod": acct.get("MOD", ""),
                    "user": acct.get("USER", ""),
                })

    @staticmethod
    def _calc_grid(n, log_w, log_h, min_w):
        if n <= 0:
            return 1, 1
        if n == 1:
            return 1, 1
        if n <= 2:
            return (2, 1) if log_w // 2 >= min_w else (1, 2)
        if n <= 4:
            return 2, math.ceil(n / 2)
        if n <= 6:
            return 3, math.ceil(n / 3)
        return 4, math.ceil(n / 4)

    def _get_scale_and_offset(self):
        """Calculate a unified scale factor and offset to fit all monitors on canvas."""
        cw = self.canvas.winfo_width()
        ch = self.canvas.winfo_height()
        if cw < 10 or ch < 10:
            return 1, 0, 0

        if not self._monitors:
            return 1, 0, 0

        # Find bounding box of all monitors in logical coords
        min_x = float("inf")
        min_y = float("inf")
        max_x = float("-inf")
        max_y = float("-inf")

        for mid, mon in self._monitors.items():
            scale = max(mon.get("SCALE", 100), 1)
            lw = mon["W"] * 100 // scale
            lh = mon["H"] * 100 // scale
            mx = mon.get("X", 0)
            my = mon.get("Y", 0)
            min_x = min(min_x, mx)
            min_y = min(min_y, my)
            max_x = max(max_x, mx + lw)
            max_y = max(max_y, my + lh)

        total_w = max(max_x - min_x, 1)
        total_h = max(max_y - min_y, 1)

        margin = 40
        avail_w = cw - 2 * margin
        avail_h = ch - 2 * margin

        s = min(avail_w / total_w, avail_h / total_h)
        ox = margin + (avail_w - total_w * s) / 2 - min_x * s
        oy = margin + (avail_h - total_h * s) / 2 - min_y * s

        return s, ox, oy

    def _redraw(self):
        self.canvas.delete("all")
        self._canvas_items.clear()

        s, ox, oy = self._get_scale_and_offset()

        # Draw monitors
        for mid, mon in self._monitors.items():
            scale = max(mon.get("SCALE", 100), 1)
            lw = mon["W"] * 100 // scale
            lh = mon["H"] * 100 // scale
            mx = mon.get("X", 0)
            my = mon.get("Y", 0)
            x1 = ox + mx * s
            y1 = oy + my * s
            x2 = x1 + lw * s
            y2 = y1 + lh * s

            self.canvas.create_rectangle(
                x1, y1, x2, y2,
                fill=_MON_BG, outline=_MON_BORDER, width=2, dash=(4, 2),
            )
            self.canvas.create_text(
                x1 + 6, y1 + 4, anchor=tk.NW,
                text=f"Display {mid} ({mon['W']}x{mon['H']} @{scale}%)",
                fill=_MON_LABEL_COLOR, font=("", 9),
            )

        # Draw windows (non-play first, then play on top)
        sorted_wins = sorted(self._windows, key=lambda w: w["primary"])
        for win in sorted_wins:
            self._draw_window(win, s, ox, oy)

    def _draw_window(self, win, s, ox, oy):
        x1 = ox + win["x"] * s
        y1 = oy + win["y"] * s
        x2 = x1 + win["w"] * s
        y2 = y1 + win["h"] * s

        is_pri = win["primary"]
        fill = _PLAY_COLOR if is_pri else _FILL_COLOR
        outline = _PLAY_BORDER if is_pri else _FILL_BORDER
        line_w = 3 if is_pri else 1

        rect = self.canvas.create_rectangle(
            x1, y1, x2, y2, fill=fill, outline=outline, width=line_w,
        )

        label = self.i18n.t("play_label") if is_pri else self.i18n.t("non_play_label")
        line1 = f"ACC {win['id']}"
        line2 = f"[{label}]"
        line3 = f"{win['w']}x{win['h']}"

        cx = (x1 + x2) / 2
        cy = (y1 + y2) / 2
        text = self.canvas.create_text(
            cx, cy, text=f"{line1}\n{line2}\n{line3}",
            fill=_TEXT_COLOR, font=("", 8, "bold"), justify=tk.CENTER,
        )

        self._canvas_items[win["id"]] = (rect, text, win)

        # Bind drag events
        for item in (rect, text):
            self.canvas.tag_bind(item, "<ButtonPress-1>", lambda e, w=win: self._on_press(e, w))
            self.canvas.tag_bind(item, "<B1-Motion>", lambda e, w=win: self._on_drag(e, w))
            self.canvas.tag_bind(item, "<ButtonRelease-1>", lambda e, w=win: self._on_release(e, w))

    def _on_press(self, event, win):
        s, ox, oy = self._get_scale_and_offset()
        self._drag_data = {
            "win": win,
            "start_cx": event.x,
            "start_cy": event.y,
            "orig_x": win["x"],
            "orig_y": win["y"],
            "scale": s,
        }
        # Highlight
        if win["id"] in self._canvas_items:
            rect, text, _ = self._canvas_items[win["id"]]
            self.canvas.itemconfig(rect, outline=_DRAG_COLOR, width=3)

    def _on_drag(self, event, win):
        if not self._drag_data:
            return
        d = self._drag_data
        s = d["scale"]
        if s <= 0:
            return

        dx = (event.x - d["start_cx"]) / s
        dy = (event.y - d["start_cy"]) / s

        new_x = int(d["orig_x"] + dx)
        new_y = int(d["orig_y"] + dy)

        win["x"] = new_x
        win["y"] = new_y

        # Move canvas items directly instead of _redraw() which would destroy them
        if win["id"] in self._canvas_items:
            rect, text, _ = self._canvas_items[win["id"]]
            s2, ox, oy = self._get_scale_and_offset()
            x1 = ox + new_x * s2
            y1 = oy + new_y * s2
            x2 = x1 + win["w"] * s2
            y2 = y1 + win["h"] * s2
            self.canvas.coords(rect, x1, y1, x2, y2)
            self.canvas.coords(text, (x1 + x2) / 2, (y1 + y2) / 2)

        self._info_var.set(
            self.i18n.t("position_info").format(
                x=new_x, y=new_y, w=win["w"], h=win["h"],
            )
        )

    def _on_release(self, event, win):
        if self._drag_data:
            # Store custom position
            self._custom_positions[win["id"]] = {"x": win["x"], "y": win["y"]}
            self._drag_data = None
            self._redraw()

    def _auto_layout(self):
        """Recalculate all positions using the grid algorithm."""
        self._custom_positions.clear()
        self._calc_auto_positions()
        self._redraw()

    def _reset_positions(self):
        """Reset all custom positions."""
        self._custom_positions.clear()
        self._calc_auto_positions()
        self._redraw()

    def get_custom_positions(self):
        """Return custom positions set by dragging."""
        return dict(self._custom_positions)

    def refresh_labels(self):
        self._btn_auto.config(text=self.i18n.t("auto_layout"))
        self._btn_reset.config(text=self.i18n.t("reset_positions"))
        self._redraw()
