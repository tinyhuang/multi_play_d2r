"""Monitor settings tab: add/remove displays, auto-detect, arrange."""

import tkinter as tk
from tkinter import ttk, messagebox


class MonitorsTab:
    def __init__(self, parent, i18n):
        self.i18n = i18n
        self.frame = ttk.Frame(parent, padding=12)
        self._mon_frames = {}  # mon_id -> dict of widgets
        self._mon_ids = []

        self._build_toolbar()

        # Scrollable area for monitor panels
        container = ttk.Frame(self.frame)
        container.pack(fill=tk.BOTH, expand=True, pady=(8, 0))

        canvas = tk.Canvas(container, borderwidth=0, highlightthickness=0)
        scrollbar = ttk.Scrollbar(container, orient=tk.VERTICAL, command=canvas.yview)
        self._scroll_frame = ttk.Frame(canvas)
        self._scroll_frame.bind(
            "<Configure>",
            lambda e: canvas.configure(scrollregion=canvas.bbox("all")),
        )
        canvas.create_window((0, 0), window=self._scroll_frame, anchor=tk.NW)
        canvas.configure(yscrollcommand=scrollbar.set)
        canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self._scroll_canvas = canvas

    # ----- toolbar -----

    def _build_toolbar(self):
        bar = ttk.Frame(self.frame)
        bar.pack(fill=tk.X)

        self._btn_add = ttk.Button(
            bar, text=self.i18n.t("add_monitor"), command=self._on_add,
        )
        self._btn_add.pack(side=tk.LEFT, padx=(0, 4))

        self._btn_detect = ttk.Button(
            bar, text=self.i18n.t("auto_detect"), command=self._on_detect,
        )
        self._btn_detect.pack(side=tk.LEFT, padx=4)

        sep = ttk.Separator(bar, orient=tk.VERTICAL)
        sep.pack(side=tk.LEFT, fill=tk.Y, padx=8, pady=2)

        self._lbl_arrange = ttk.Label(bar, text=self.i18n.t("arrangement"))
        self._lbl_arrange.pack(side=tk.LEFT, padx=(0, 4))

        self._btn_h = ttk.Button(
            bar, text=self.i18n.t("side_by_side"),
            command=lambda: self._arrange("h"),
        )
        self._btn_h.pack(side=tk.LEFT, padx=2)

        self._btn_v = ttk.Button(
            bar, text=self.i18n.t("stacked"),
            command=lambda: self._arrange("v"),
        )
        self._btn_v.pack(side=tk.LEFT, padx=2)

    # ----- add / remove -----

    def _on_add(self):
        existing = set(int(x) for x in self._mon_ids if x.isdigit())
        new_id = 1
        while new_id in existing:
            new_id += 1
        mid = str(new_id)
        self._mon_ids.append(mid)
        data = {
            "W": "1920", "H": "1080", "SCALE": "100",
            "X": "0", "Y": "0", "TASKBAR": "0",
        }
        self._add_monitor_panel(mid, data)
        self._arrange("h")

    def _on_remove(self, mon_id):
        if len(self._mon_ids) <= 1:
            return
        if mon_id in self._mon_frames:
            self._mon_frames[mon_id]["lf"].destroy()
            del self._mon_frames[mon_id]
        if mon_id in self._mon_ids:
            self._mon_ids.remove(mon_id)

    # ----- auto-detect -----

    def _on_detect(self):
        from gui.monitor_detect import detect_monitors

        result = detect_monitors()
        if result is None:
            messagebox.showinfo(
                self.i18n.t("auto_detect"),
                self.i18n.t("detect_not_available"),
            )
            return
        if not result:
            messagebox.showwarning(
                self.i18n.t("auto_detect"),
                self.i18n.t("detect_failed"),
            )
            return

        # Rebuild panels from detected monitors
        for child in self._scroll_frame.winfo_children():
            child.destroy()
        self._mon_frames.clear()
        self._mon_ids = []

        for i, mon in enumerate(result, start=1):
            mid = str(i)
            self._mon_ids.append(mid)
            self._add_monitor_panel(mid, mon)

    # ----- arrangement presets -----

    def _arrange(self, mode):
        """Auto-calculate X/Y offsets: 'h' = side-by-side, 'v' = stacked."""
        if not self._mon_ids:
            return

        offset = 0
        for mid in self._mon_ids:
            if mid not in self._mon_frames:
                continue
            entries = self._mon_frames[mid]["entries"]
            w = int(entries["W"].get() or "1920")
            h = int(entries["H"].get() or "1080")
            scale = max(int(entries["SCALE"].get() or "100"), 1)

            # Set position
            _set_entry(entries["X"], str(offset if mode == "h" else 0))
            _set_entry(entries["Y"], str(offset if mode == "v" else 0))

            # Advance offset by logical size
            log_w = w * 100 // scale
            log_h = h * 100 // scale
            offset += log_w if mode == "h" else log_h

    # ----- panel building -----

    def _add_monitor_panel(self, mon_id, data):
        lf = ttk.LabelFrame(
            self._scroll_frame,
            text=self.i18n.t("display_n").format(n=mon_id),
            padding=8,
        )
        lf.pack(fill=tk.X, pady=(0, 8))

        entries = {}

        # Row 0: Resolution + Scale + Remove button
        r0 = ttk.Frame(lf)
        r0.pack(fill=tk.X, pady=2)

        lbl_res = ttk.Label(r0, text=self.i18n.t("resolution"), width=14)
        lbl_res.pack(side=tk.LEFT)
        w_entry = ttk.Entry(r0, width=7)
        w_entry.pack(side=tk.LEFT)
        w_entry.insert(0, str(data.get("W", "1920")))
        ttk.Label(r0, text=" x ").pack(side=tk.LEFT)
        h_entry = ttk.Entry(r0, width=7)
        h_entry.pack(side=tk.LEFT)
        h_entry.insert(0, str(data.get("H", "1080")))

        ttk.Label(r0, text="    ").pack(side=tk.LEFT)
        lbl_scale = ttk.Label(r0, text=self.i18n.t("dpi_scale"), width=14)
        lbl_scale.pack(side=tk.LEFT)
        scale_entry = ttk.Entry(r0, width=6)
        scale_entry.pack(side=tk.LEFT)
        scale_entry.insert(0, str(data.get("SCALE", "100")))

        # Remove button at right end
        btn_rm = ttk.Button(
            r0, text=self.i18n.t("remove_monitor"), width=6,
            command=lambda mid=mon_id: self._on_remove(mid),
        )
        btn_rm.pack(side=tk.RIGHT)

        entries["W"] = w_entry
        entries["H"] = h_entry
        entries["SCALE"] = scale_entry

        # Row 1: Offset + Taskbar
        r1 = ttk.Frame(lf)
        r1.pack(fill=tk.X, pady=2)

        lbl_offset = ttk.Label(r1, text=self.i18n.t("offset_xy"), width=14)
        lbl_offset.pack(side=tk.LEFT)
        x_entry = ttk.Entry(r1, width=7)
        x_entry.pack(side=tk.LEFT)
        x_entry.insert(0, str(data.get("X", "0")))
        ttk.Label(r1, text=" / ").pack(side=tk.LEFT)
        y_entry = ttk.Entry(r1, width=7)
        y_entry.pack(side=tk.LEFT)
        y_entry.insert(0, str(data.get("Y", "0")))

        ttk.Label(r1, text="    ").pack(side=tk.LEFT)
        lbl_tb = ttk.Label(r1, text=self.i18n.t("taskbar_height"), width=14)
        lbl_tb.pack(side=tk.LEFT)
        tb_entry = ttk.Entry(r1, width=6)
        tb_entry.pack(side=tk.LEFT)
        tb_entry.insert(0, str(data.get("TASKBAR", "0")))

        entries["X"] = x_entry
        entries["Y"] = y_entry
        entries["TASKBAR"] = tb_entry

        self._mon_frames[mon_id] = {
            "lf": lf,
            "entries": entries,
            "btn_rm": btn_rm,
            "labels": {
                "res": lbl_res, "scale": lbl_scale,
                "offset": lbl_offset, "tb": lbl_tb,
            },
        }

    def _get_monitor_data(self, mon_id):
        if mon_id not in self._mon_frames:
            return {}
        entries = self._mon_frames[mon_id]["entries"]
        return {key: entry.get() for key, entry in entries.items()}

    # ----- public API -----

    def load(self, base):
        monitors = base.get("_monitors", {})
        ids_str = base.get("MONITOR_IDS", "1")

        self._mon_ids = ids_str.split()
        for child in self._scroll_frame.winfo_children():
            child.destroy()
        self._mon_frames.clear()

        for mid in self._mon_ids:
            data = monitors.get(mid, {
                "W": 1920, "H": 1080, "SCALE": 100,
                "X": 0, "Y": 0, "TASKBAR": 0,
            })
            self._add_monitor_panel(mid, data)

    def get_monitors(self):
        result = {}
        for mid in self._mon_ids:
            data = self._get_monitor_data(mid)
            result[mid] = {
                "W": int(data.get("W", "1920") or "1920"),
                "H": int(data.get("H", "1080") or "1080"),
                "SCALE": int(data.get("SCALE", "100") or "100"),
                "X": int(data.get("X", "0") or "0"),
                "Y": int(data.get("Y", "0") or "0"),
                "TASKBAR": int(data.get("TASKBAR", "0") or "0"),
            }
        return result

    def refresh_labels(self):
        self._btn_add.config(text=self.i18n.t("add_monitor"))
        self._btn_detect.config(text=self.i18n.t("auto_detect"))
        self._lbl_arrange.config(text=self.i18n.t("arrangement"))
        self._btn_h.config(text=self.i18n.t("side_by_side"))
        self._btn_v.config(text=self.i18n.t("stacked"))
        for mid, widgets in self._mon_frames.items():
            widgets["lf"].config(text=self.i18n.t("display_n").format(n=mid))
            widgets["btn_rm"].config(text=self.i18n.t("remove_monitor"))
            lbls = widgets["labels"]
            lbls["res"].config(text=self.i18n.t("resolution"))
            lbls["scale"].config(text=self.i18n.t("dpi_scale"))
            lbls["offset"].config(text=self.i18n.t("offset_xy"))
            lbls["tb"].config(text=self.i18n.t("taskbar_height"))


def _set_entry(entry, value):
    entry.delete(0, tk.END)
    entry.insert(0, value)
