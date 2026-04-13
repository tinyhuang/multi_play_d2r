"""Internationalization module for D2R Multi-Play Launcher GUI."""


class I18n:
    """Simple dictionary-based i18n with EN/CN support."""

    def __init__(self, lang="en"):
        self._lang = lang
        self._callbacks = []

    @property
    def lang(self):
        return self._lang

    def set_lang(self, lang):
        self._lang = lang
        for cb in self._callbacks:
            cb()

    def on_lang_change(self, callback):
        self._callbacks.append(callback)

    def t(self, key):
        return STRINGS.get(self._lang, STRINGS["en"]).get(key, key)


STRINGS = {
    "en": {
        # Window
        "app_title": "D2R Multi-Play Launcher",
        "language": "Language",
        "lang_en": "English",
        "lang_zh": "中文",

        # Tabs
        "tab_global": "Global Settings",
        "tab_monitors": "Monitors",
        "tab_accounts": "Accounts",
        "tab_preview": "Layout Preview",

        # Global settings
        "server_address": "Server Address",
        "launch_interval": "Launch Interval (sec)",
        "diablo_path": "D2R Executable",
        "workdir": "Tools Directory",
        "browse": "Browse...",
        "primary_win_size": "Play Window Size",
        "default_win_size": "Non-Play Window Size",
        "min_win_size": "Minimum Window Size",

        # Monitors
        "monitor_ids": "Display IDs (match Windows Settings)",
        "add_monitor": "+ Add",
        "remove_monitor": "- Remove",
        "display_n": "Display {n}",
        "resolution": "Resolution",
        "dpi_scale": "DPI Scale (%)",
        "offset_xy": "Offset X / Y",
        "taskbar_height": "Taskbar Height (px)",

        # Accounts
        "account_n": "Account {n}",
        "enabled": "Enabled",
        "primary": "Play",
        "monitor": "Display",
        "auto_assign": "Auto",
        "username": "Username",
        "password": "Password",
        "mod": "Mod",
        "options": "Options",
        "diablo_override": "D2R Path Override",
        "show_password": "Show",
        "hide_password": "Hide",

        # Preview
        "auto_layout": "Auto Layout",
        "reset_positions": "Reset Positions",
        "position_info": "Pos: {x},{y}  Size: {w}x{h}",
        "no_accounts": "No enabled accounts",
        "play_label": "PLAY",
        "non_play_label": "fill",

        # Actions
        "save_config": "Save Config",
        "launch": "Launch",
        "save_and_launch": "Save & Launch",
        "status_ready": "Ready",
        "status_saved": "Configuration saved.",
        "status_launched": "Launched! Check game windows.",
        "status_error": "Error: {msg}",
        "confirm_launch": "Save config and launch D2R instances?",
        "confirm_title": "Confirm Launch",

        # Errors
        "err_no_config_dir": "Config directory not found: {path}",
        "err_write_fail": "Failed to write config: {msg}",
        "err_launch_fail": "Failed to launch: {msg}",
    },
    "zh": {
        # Window
        "app_title": "D2R 多开启动器",
        "language": "语言",
        "lang_en": "English",
        "lang_zh": "中文",

        # Tabs
        "tab_global": "全局设置",
        "tab_monitors": "显示器",
        "tab_accounts": "账号配置",
        "tab_preview": "布局预览",

        # Global settings
        "server_address": "服务器地址",
        "launch_interval": "启动间隔（秒）",
        "diablo_path": "D2R 可执行文件",
        "workdir": "工具目录",
        "browse": "浏览...",
        "primary_win_size": "游玩窗口大小",
        "default_win_size": "辅助窗口大小",
        "min_win_size": "最小窗口大小",

        # Monitors
        "monitor_ids": "显示器编号（与 Windows 屏幕设置一致）",
        "add_monitor": "+ 添加",
        "remove_monitor": "- 删除",
        "display_n": "显示器 {n}",
        "resolution": "分辨率",
        "dpi_scale": "缩放比例 (%)",
        "offset_xy": "偏移 X / Y",
        "taskbar_height": "任务栏高度（像素）",

        # Accounts
        "account_n": "账号 {n}",
        "enabled": "启用",
        "primary": "游玩",
        "monitor": "显示器",
        "auto_assign": "自动",
        "username": "账号",
        "password": "密码",
        "mod": "Mod",
        "options": "参数",
        "diablo_override": "独立 D2R 路径",
        "show_password": "显示",
        "hide_password": "隐藏",

        # Preview
        "auto_layout": "自动布局",
        "reset_positions": "重置位置",
        "position_info": "坐标: {x},{y}  大小: {w}x{h}",
        "no_accounts": "无已启用账号",
        "play_label": "游玩",
        "non_play_label": "辅助",

        # Actions
        "save_config": "保存配置",
        "launch": "启动",
        "save_and_launch": "保存并启动",
        "status_ready": "就绪",
        "status_saved": "配置已保存。",
        "status_launched": "已启动！请查看游戏窗口。",
        "status_error": "错误：{msg}",
        "confirm_launch": "保存配置并启动 D2R 实例？",
        "confirm_title": "确认启动",

        # Errors
        "err_no_config_dir": "配置目录未找到：{path}",
        "err_write_fail": "写入配置失败：{msg}",
        "err_launch_fail": "启动失败：{msg}",
    },
}
