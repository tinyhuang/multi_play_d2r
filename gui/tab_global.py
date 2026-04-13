"""Global settings tab: server, paths, window sizes."""

import tkinter as tk
from tkinter import ttk, filedialog


class GlobalTab:
    def __init__(self, parent, i18n):
        self.i18n = i18n
        self.frame = ttk.Frame(parent, padding=12)

        self._labels = {}
        self._entries = {}
        self._build()

    def _build(self):
        f = self.frame
        row = 0

        fields = [
            ("server_address", "addres", None),
            ("launch_interval", "secs", None),
            ("diablo_path", "diablo", "file"),
            ("workdir", "workdir", "dir"),
        ]
        for label_key, var_key, browse_type in fields:
            lbl = ttk.Label(f, text=self.i18n.t(label_key), width=22, anchor=tk.W)
            lbl.grid(row=row, column=0, sticky=tk.W, pady=3)
            self._labels[label_key] = lbl

            entry = ttk.Entry(f, width=50)
            entry.grid(row=row, column=1, sticky=tk.EW, padx=(4, 0), pady=3)
            self._entries[var_key] = entry

            if browse_type:
                btn = ttk.Button(
                    f, text=self.i18n.t("browse"), width=8,
                    command=lambda e=entry, bt=browse_type: self._browse(e, bt),
                )
                btn.grid(row=row, column=2, padx=(4, 0), pady=3)
            row += 1

        # Separator
        ttk.Separator(f, orient=tk.HORIZONTAL).grid(
            row=row, column=0, columnspan=3, sticky=tk.EW, pady=10,
        )
        row += 1

        size_fields = [
            ("primary_win_size", "PRIMARY_WIN_W", "PRIMARY_WIN_H"),
            ("default_win_size", "DEFAULT_WIN_W", "DEFAULT_WIN_H"),
            ("min_win_size", "MIN_WIN_W", "MIN_WIN_H"),
        ]
        for label_key, w_key, h_key in size_fields:
            lbl = ttk.Label(f, text=self.i18n.t(label_key), width=22, anchor=tk.W)
            lbl.grid(row=row, column=0, sticky=tk.W, pady=3)
            self._labels[label_key] = lbl

            size_frame = ttk.Frame(f)
            size_frame.grid(row=row, column=1, sticky=tk.W, padx=(4, 0), pady=3)

            w_entry = ttk.Entry(size_frame, width=8)
            w_entry.pack(side=tk.LEFT)
            ttk.Label(size_frame, text=" x ").pack(side=tk.LEFT)
            h_entry = ttk.Entry(size_frame, width=8)
            h_entry.pack(side=tk.LEFT)

            self._entries[w_key] = w_entry
            self._entries[h_key] = h_entry
            row += 1

        f.columnconfigure(1, weight=1)

    def _browse(self, entry, browse_type):
        if browse_type == "file":
            path = filedialog.askopenfilename(
                filetypes=[("Executable", "*.exe"), ("All files", "*.*")],
            )
        else:
            path = filedialog.askdirectory()
        if path:
            entry.delete(0, tk.END)
            entry.insert(0, path)

    def load(self, base):
        mapping = {
            "addres": base.get("addres", ""),
            "secs": base.get("secs", "8"),
            "diablo": base.get("diablo", ""),
            "workdir": base.get("workdir", ""),
            "DEFAULT_WIN_W": base.get("DEFAULT_WIN_W", "1280"),
            "DEFAULT_WIN_H": base.get("DEFAULT_WIN_H", "720"),
            "MIN_WIN_W": base.get("MIN_WIN_W", "800"),
            "MIN_WIN_H": base.get("MIN_WIN_H", "600"),
            "PRIMARY_WIN_W": base.get("PRIMARY_WIN_W", "1920"),
            "PRIMARY_WIN_H": base.get("PRIMARY_WIN_H", "1080"),
        }
        for key, entry in self._entries.items():
            entry.delete(0, tk.END)
            entry.insert(0, mapping.get(key, ""))

    def get_settings(self):
        return {key: entry.get() for key, entry in self._entries.items()}

    def refresh_labels(self):
        label_keys = [
            "server_address", "launch_interval", "diablo_path", "workdir",
            "primary_win_size", "default_win_size", "min_win_size",
        ]
        for key in label_keys:
            if key in self._labels:
                self._labels[key].config(text=self.i18n.t(key))
