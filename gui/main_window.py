"""Main window with tabbed interface and language switcher."""

import tkinter as tk
from tkinter import ttk

from gui.i18n import I18n
from gui.tab_global import GlobalTab
from gui.tab_monitors import MonitorsTab
from gui.tab_accounts import AccountsTab
from gui.tab_preview import PreviewTab
from gui.launcher import Launcher


class MainWindow:
    def __init__(self, config_dir):
        self.config_dir = config_dir
        self.i18n = I18n("en")
        self.launcher = Launcher(config_dir)

        self.root = tk.Tk()
        self.root.title(self.i18n.t("app_title"))
        self.root.geometry("1100x720")
        self.root.minsize(900, 600)

        self._build_toolbar()
        self._build_tabs()
        self._build_status_bar()
        self._build_action_bar()

        self.i18n.on_lang_change(self._refresh_ui)
        self._load_config()

    def _build_toolbar(self):
        toolbar = ttk.Frame(self.root)
        toolbar.pack(fill=tk.X, padx=8, pady=(8, 0))

        self._lang_label = ttk.Label(toolbar, text=self.i18n.t("language"))
        self._lang_label.pack(side=tk.LEFT)

        self._lang_var = tk.StringVar(value="en")
        lang_en = ttk.Radiobutton(
            toolbar, text="English", variable=self._lang_var,
            value="en", command=self._on_lang_change,
        )
        lang_zh = ttk.Radiobutton(
            toolbar, text="中文", variable=self._lang_var,
            value="zh", command=self._on_lang_change,
        )
        lang_en.pack(side=tk.LEFT, padx=(8, 2))
        lang_zh.pack(side=tk.LEFT, padx=2)

    def _build_tabs(self):
        self.notebook = ttk.Notebook(self.root)
        self.notebook.pack(fill=tk.BOTH, expand=True, padx=8, pady=4)

        self.tab_global = GlobalTab(self.notebook, self.i18n)
        self.tab_monitors = MonitorsTab(self.notebook, self.i18n)
        self.tab_accounts = AccountsTab(self.notebook, self.i18n)
        self.tab_preview = PreviewTab(self.notebook, self.i18n)

        self.notebook.add(self.tab_global.frame, text=self.i18n.t("tab_global"))
        self.notebook.add(self.tab_monitors.frame, text=self.i18n.t("tab_monitors"))
        self.notebook.add(self.tab_accounts.frame, text=self.i18n.t("tab_accounts"))
        self.notebook.add(self.tab_preview.frame, text=self.i18n.t("tab_preview"))

        self.notebook.bind("<<NotebookTabChanged>>", self._on_tab_changed)

    def _build_status_bar(self):
        self._status_var = tk.StringVar(value=self.i18n.t("status_ready"))
        status = ttk.Label(self.root, textvariable=self._status_var, relief=tk.SUNKEN)
        status.pack(fill=tk.X, side=tk.BOTTOM, padx=8, pady=(0, 4))

    def _build_action_bar(self):
        bar = ttk.Frame(self.root)
        bar.pack(fill=tk.X, side=tk.BOTTOM, padx=8, pady=4)

        self._btn_save = ttk.Button(
            bar, text=self.i18n.t("save_config"), command=self._on_save,
        )
        self._btn_save.pack(side=tk.LEFT, padx=(0, 8))

        self._btn_launch = ttk.Button(
            bar, text=self.i18n.t("save_and_launch"), command=self._on_launch,
        )
        self._btn_launch.pack(side=tk.LEFT)

    def _on_lang_change(self):
        self.i18n.set_lang(self._lang_var.get())

    def _on_tab_changed(self, event):
        idx = self.notebook.index("current")
        if idx == 3:  # Preview tab
            self._update_preview()

    def _load_config(self):
        from gui.config_parser import load_base_settings, load_accounts
        self._base = load_base_settings(self.config_dir)
        self._accts = load_accounts(self.config_dir)
        self.tab_global.load(self._base)
        self.tab_monitors.load(self._base)
        self.tab_accounts.load(self._accts["accounts"])

    def _update_preview(self):
        monitors = self.tab_monitors.get_monitors()
        accounts = self.tab_accounts.get_accounts()
        global_cfg = self.tab_global.get_settings()
        self.tab_preview.update_layout(monitors, accounts, global_cfg)

    def _collect_all(self):
        settings = self.tab_global.get_settings()
        monitors = self.tab_monitors.get_monitors()
        accounts = self.tab_accounts.get_accounts()
        # Apply preview drag positions if any
        positions = self.tab_preview.get_custom_positions()
        for acct in accounts:
            aid = str(acct["id"])
            if aid in positions:
                acct["WIN_X"] = str(positions[aid]["x"])
                acct["WIN_Y"] = str(positions[aid]["y"])
        return settings, monitors, accounts

    def _on_save(self):
        try:
            from gui.config_parser import save_base_settings, save_accounts
            settings, monitors, accounts = self._collect_all()
            save_base_settings(self.config_dir, settings, monitors)
            save_accounts(self.config_dir, accounts)
            self._status_var.set(self.i18n.t("status_saved"))
        except Exception as e:
            self._status_var.set(self.i18n.t("status_error").format(msg=str(e)))

    def _on_launch(self):
        from tkinter import messagebox
        if not messagebox.askyesno(
            self.i18n.t("confirm_title"), self.i18n.t("confirm_launch")
        ):
            return
        self._on_save()
        try:
            self.launcher.launch()
            self._status_var.set(self.i18n.t("status_launched"))
        except Exception as e:
            self._status_var.set(self.i18n.t("status_error").format(msg=str(e)))

    def _refresh_ui(self):
        self.root.title(self.i18n.t("app_title"))
        self._lang_label.config(text=self.i18n.t("language"))
        self._btn_save.config(text=self.i18n.t("save_config"))
        self._btn_launch.config(text=self.i18n.t("save_and_launch"))
        self._status_var.set(self.i18n.t("status_ready"))
        self.notebook.tab(0, text=self.i18n.t("tab_global"))
        self.notebook.tab(1, text=self.i18n.t("tab_monitors"))
        self.notebook.tab(2, text=self.i18n.t("tab_accounts"))
        self.notebook.tab(3, text=self.i18n.t("tab_preview"))
        self.tab_global.refresh_labels()
        self.tab_monitors.refresh_labels()
        self.tab_accounts.refresh_labels()
        self.tab_preview.refresh_labels()

    def run(self):
        self.root.mainloop()
