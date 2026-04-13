"""Account configuration tab: 8-row spreadsheet-style editor."""

import tkinter as tk
from tkinter import ttk


class AccountsTab:
    def __init__(self, parent, i18n):
        self.i18n = i18n
        self.frame = ttk.Frame(parent, padding=12)
        self._rows = []  # list of dicts per account
        self._header_labels = []
        self._build()

    def _build(self):
        # Scrollable canvas for the table
        canvas = tk.Canvas(self.frame, borderwidth=0)
        scrollbar = ttk.Scrollbar(self.frame, orient=tk.VERTICAL, command=canvas.yview)
        self._table_frame = ttk.Frame(canvas)
        self._table_frame.bind(
            "<Configure>",
            lambda e: canvas.configure(scrollregion=canvas.bbox("all")),
        )
        canvas.create_window((0, 0), window=self._table_frame, anchor=tk.NW)
        canvas.configure(yscrollcommand=scrollbar.set)
        canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        self._build_header()
        for i in range(1, 9):
            self._build_row(i)

    def _build_header(self):
        headers = [
            ("#", 3), ("enabled", 5), ("primary", 5), ("monitor", 6),
            ("username", 20), ("password", 14), ("mod", 8),
            ("options", 12), ("diablo_override", 20),
        ]
        for col, (key, width) in enumerate(headers):
            lbl = ttk.Label(
                self._table_frame, text=self.i18n.t(key) if key != "#" else "#",
                font=("", 9, "bold"), width=width, anchor=tk.CENTER,
            )
            lbl.grid(row=0, column=col, padx=2, pady=(0, 4), sticky=tk.EW)
            self._header_labels.append((key, lbl))

    def _build_row(self, idx):
        row = {}
        r = idx  # grid row (0 is header)

        # #
        ttk.Label(self._table_frame, text=str(idx), width=3, anchor=tk.CENTER).grid(
            row=r, column=0, padx=2, pady=1,
        )

        # Enabled checkbox
        row["enable_var"] = tk.BooleanVar(value=False)
        cb = ttk.Checkbutton(self._table_frame, variable=row["enable_var"])
        cb.grid(row=r, column=1, padx=2, pady=1)

        # Primary checkbox
        row["primary_var"] = tk.BooleanVar(value=False)
        cb2 = ttk.Checkbutton(self._table_frame, variable=row["primary_var"])
        cb2.grid(row=r, column=2, padx=2, pady=1)

        # Monitor
        row["monitor_entry"] = ttk.Entry(self._table_frame, width=6)
        row["monitor_entry"].grid(row=r, column=3, padx=2, pady=1)

        # Username
        row["user_entry"] = ttk.Entry(self._table_frame, width=20)
        row["user_entry"].grid(row=r, column=4, padx=2, pady=1)

        # Password (masked)
        row["pass_entry"] = ttk.Entry(self._table_frame, width=14, show="*")
        row["pass_entry"].grid(row=r, column=5, padx=2, pady=1)

        # Mod
        row["mod_entry"] = ttk.Entry(self._table_frame, width=8)
        row["mod_entry"].grid(row=r, column=6, padx=2, pady=1)

        # Options
        row["options_entry"] = ttk.Entry(self._table_frame, width=12)
        row["options_entry"].grid(row=r, column=7, padx=2, pady=1)

        # Diablo override
        row["diablo_entry"] = ttk.Entry(self._table_frame, width=20)
        row["diablo_entry"].grid(row=r, column=8, padx=2, pady=1)

        row["id"] = idx
        self._rows.append(row)

    def load(self, accounts):
        for i, acct in enumerate(accounts):
            if i >= len(self._rows):
                break
            row = self._rows[i]
            row["enable_var"].set(acct.get("ENABLE", "0").strip()[:1] == "1")
            row["primary_var"].set(acct.get("PRIMARY", "0").strip()[:1] == "1")

            _set_entry(row["monitor_entry"], acct.get("MONITOR", ""))
            _set_entry(row["user_entry"], acct.get("USER", ""))
            _set_entry(row["pass_entry"], acct.get("PASS", ""))
            _set_entry(row["mod_entry"], acct.get("MOD", ""))
            _set_entry(row["options_entry"], acct.get("OPTIONS", ""))
            _set_entry(row["diablo_entry"], acct.get("DIABLO", ""))

    def get_accounts(self):
        result = []
        for row in self._rows:
            result.append({
                "id": row["id"],
                "ENABLE": "1" if row["enable_var"].get() else "0",
                "PRIMARY": "1" if row["primary_var"].get() else "0",
                "MONITOR": row["monitor_entry"].get().strip(),
                "USER": row["user_entry"].get(),
                "PASS": row["pass_entry"].get(),
                "MOD": row["mod_entry"].get(),
                "OPTIONS": row["options_entry"].get(),
                "DIABLO": row["diablo_entry"].get(),
                "WIN_X": "",
                "WIN_Y": "",
            })
        return result

    def refresh_labels(self):
        for key, lbl in self._header_labels:
            if key != "#":
                lbl.config(text=self.i18n.t(key))


def _set_entry(entry, value):
    entry.delete(0, tk.END)
    entry.insert(0, value)
