// ============================================================
// MainForm.cs — 主窗口
// 账号列表 + 状态灯 + 启动/重连按钮 + 日志面板
// ============================================================

using System.ComponentModel;
using System.Diagnostics;
using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Guard;
using D2RMultiPlay.Core.Handles;
using D2RMultiPlay.Core.Launch;
using D2RMultiPlay.Core.Monitors;
using D2RMultiPlay.Core.Windows;

namespace D2RMultiPlay.App;

public partial class MainForm : Form
{
    private AppConfig _config;
    private readonly ProcessGuard _guard = new();

    // ---- UI 控件 ----
    private MenuStrip _menuStrip = null!;
    private DataGridView _grid = null!;
    private RichTextBox _logBox = null!;
    private Button _btnLaunchAll = null!;
    private Button _btnStopAll = null!;
    private Button _btnAddAccount = null!;
    private Button _btnLayout = null!;
    private Button _btnSettings = null!;
    private Button _btnLangZh = null!;
    private Button _btnLangEn = null!;
    private TextBox _txtD2rPath = null!;
    private Button _btnBrowseD2r = null!;
    private Label _lblD2rStatus = null!;
    private TextBox _txtHandlePath = null!;
    private Button _btnBrowseHandle = null!;
    private LinkLabel _lnkHandleDownload = null!;
    private Label _lblHandleStatus = null!;
    private SplitContainer _splitContainer = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public MainForm(AppConfig config)
    {
        _config = config;
        InitializeComponent();
        SetupMenu();
        SetupGrid();
        SetupButtons();
        SetupLog();
        SetupStatusBar();
        SetupGuard();
        RefreshGrid();
        ApplyLocalization();
    }

    // ======== 初始化 ========

    private void InitializeComponent()
    {
        Text = Strings.AppTitle;
        Size = new Size(900, 650);
        MinimumSize = new Size(700, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 380,
            Panel1MinSize = 200,
            Panel2MinSize = 100
        };
        Controls.Add(_splitContainer);
    }

    private void SetupMenu()
    {
        _menuStrip = new MenuStrip();

        // 文件菜单
        var fileMenu = new ToolStripMenuItem(Strings.MenuFile);
        fileMenu.DropDownItems.Add(Strings.MenuGlobalSettings, null, (_, _) => ShowGlobalSettings());
        fileMenu.DropDownItems.Add(Strings.MenuLayout, null, (_, _) => ShowMonitorLayout());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(Strings.MenuImport, null, (_, _) => ImportConfig());
        fileMenu.DropDownItems.Add(Strings.MenuExport, null, (_, _) => ExportConfig());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(Strings.MenuExit, null, (_, _) => Close());
        _menuStrip.Items.Add(fileMenu);

        // 语言菜单
        var langMenu = new ToolStripMenuItem(Strings.MenuLanguage);
        langMenu.DropDownItems.Add("中文 (简体)", null, (_, _) => SwitchLanguage("zh-CN"));
        langMenu.DropDownItems.Add("English", null, (_, _) => SwitchLanguage("en-US"));
        _menuStrip.Items.Add(langMenu);

        // 帮助菜单
        var helpMenu = new ToolStripMenuItem(Strings.MenuHelp);
        helpMenu.DropDownItems.Add(Strings.MenuAbout, null, (_, _) => ShowAbout());
        _menuStrip.Items.Add(helpMenu);

        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);
    }

    private void SetupGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        var colId = new DataGridViewTextBoxColumn { Name = "colId", HeaderText = Strings.ColId, Width = 40, FillWeight = 7, ReadOnly = true };
        var colEnabled = new DataGridViewCheckBoxColumn { Name = "colEnabled", HeaderText = "启用", Width = 55, FillWeight = 8, ReadOnly = false };
        var colName = new DataGridViewTextBoxColumn { Name = "colName", HeaderText = Strings.ColName, FillWeight = 16, ReadOnly = true };
        var colUser = new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = Strings.ColEmail, FillWeight = 22, ReadOnly = true };
        var colRole = new DataGridViewTextBoxColumn { Name = "colRole", HeaderText = Strings.ColRole, Width = 80, FillWeight = 10, ReadOnly = true };
        var colServer = new DataGridViewTextBoxColumn { Name = "colServer", HeaderText = SServerHeader(), FillWeight = 17, ReadOnly = true };
        var colMod = new DataGridViewTextBoxColumn { Name = "colMod", HeaderText = Strings.ColMod, Width = 80, FillWeight = 9, ReadOnly = true };
        var colStatus = new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = Strings.ColStatus, Width = 80, FillWeight = 9, ReadOnly = true };
        var colEdit = new DataGridViewButtonColumn { Name = "colEdit", HeaderText = "", Text = Strings.BtnEdit, UseColumnTextForButtonValue = true, Width = 80, FillWeight = 10, ReadOnly = true };
        var colLaunch = new DataGridViewButtonColumn { Name = "colLaunch", HeaderText = "", Text = Strings.BtnLaunch, UseColumnTextForButtonValue = true, Width = 90, FillWeight = 12, ReadOnly = true };

        _grid.Columns.AddRange(colId, colEnabled, colName, colUser, colRole, colServer, colMod, colStatus, colEdit, colLaunch);

        _grid.CellClick += Grid_CellClick;
        _grid.CellContentClick += Grid_CellContentClick;
        _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
        _grid.CellValueChanged += Grid_CellValueChanged;
        _splitContainer.Panel1.Controls.Add(_grid);
    }

    private void SetupButtons()
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(6, 6, 6, 2)
        };

        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 6)
        };

        _btnLaunchAll = CreateTopButton(Strings.BtnLaunchAll);
        _btnLaunchAll.Click += async (_, _) => await LaunchAllAsync();

        _btnStopAll = CreateTopButton(Strings.BtnStopAll);
        _btnStopAll.Click += (_, _) => StopAll();

        _btnAddAccount = CreateTopButton(Strings.BtnAddAccount);
        _btnAddAccount.Click += (_, _) => AddAccount();

        _btnLayout = CreateTopButton($"{Strings.MenuLayout}");
        _btnLayout.Click += (_, _) => ShowMonitorLayout();

        _btnSettings = CreateTopButton($"{Strings.MenuGlobalSettings}");
        _btnSettings.Click += (_, _) => ShowGlobalSettings();

        _btnLangZh = CreateTopButton("中文");
        _btnLangZh.Click += (_, _) => SwitchLanguage("zh-CN");

        _btnLangEn = CreateTopButton("English");
        _btnLangEn.Click += (_, _) => SwitchLanguage("en-US");

        panel.Controls.AddRange([_btnLaunchAll, _btnStopAll, _btnAddAccount, _btnLayout, _btnSettings, _btnLangZh, _btnLangEn]);

        var quickConfig = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 5,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        quickConfig.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        quickConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        quickConfig.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        quickConfig.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        quickConfig.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        quickConfig.Controls.Add(new Label
        {
            Text = "D2R.exe 路径 / Path (必填 Required)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 7, 8, 0)
        }, 0, 0);

        _txtD2rPath = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 3) };
        _txtD2rPath.Leave += (_, _) => ApplyQuickD2rPath();
        quickConfig.Controls.Add(_txtD2rPath, 1, 0);

        _btnBrowseD2r = new Button { Text = "...", AutoSize = true, Margin = new Padding(0, 2, 6, 2) };
        _btnBrowseD2r.Click += (_, _) => BrowseD2rExe();
        quickConfig.Controls.Add(_btnBrowseD2r, 2, 0);

        quickConfig.Controls.Add(new Label { Text = "", AutoSize = true, Margin = new Padding(0, 8, 10, 0) }, 3, 0);

        _lblD2rStatus = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 0, 0) };
        quickConfig.Controls.Add(_lblD2rStatus, 4, 0);

        quickConfig.Controls.Add(new Label
        {
            Text = "handle.exe 路径 / Path",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 7, 8, 0)
        }, 0, 1);

        _txtHandlePath = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 3) };
        _txtHandlePath.Leave += (_, _) => ApplyQuickHandlePath();
        quickConfig.Controls.Add(_txtHandlePath, 1, 1);

        _btnBrowseHandle = new Button { Text = "...", AutoSize = true, Margin = new Padding(0, 2, 6, 2) };
        _btnBrowseHandle.Click += (_, _) => BrowseHandleExe();
        quickConfig.Controls.Add(_btnBrowseHandle, 2, 1);

        _lnkHandleDownload = new LinkLabel { Text = "下载 handle.exe", AutoSize = true, Margin = new Padding(0, 8, 10, 0) };
        _lnkHandleDownload.LinkClicked += (_, _) => OpenHandleDownload();
        quickConfig.Controls.Add(_lnkHandleDownload, 3, 1);

        _lblHandleStatus = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 0, 0) };
        quickConfig.Controls.Add(_lblHandleStatus, 4, 1);

        container.Controls.Add(panel, 0, 0);
        container.Controls.Add(quickConfig, 0, 1);

        _splitContainer.Panel1.Controls.Add(container);
        SyncQuickSettingsFromConfig();
    }

    private static Button CreateTopButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(110, 32),
            Margin = new Padding(0, 0, 8, 8),
            Padding = new Padding(10, 4, 10, 4)
        };
    }

    private void SetupLog()
    {
        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 9F),
            BorderStyle = BorderStyle.None
        };
        _splitContainer.Panel2.Controls.Add(_logBox);
    }

    private void SetupStatusBar()
    {
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready");
        _statusStrip.Items.Add(_statusLabel);
        Controls.Add(_statusStrip);
    }

    private void SetupGuard()
    {
        _guard.StateChanged += (_, e) =>
        {
            if (InvokeRequired)
            {
                Invoke(() => OnGuardStateChanged(e));
                return;
            }
            OnGuardStateChanged(e);
        };
        _guard.Start();
    }

    // ======== 数据刷新 ========

    private void RefreshGrid()
    {
        _grid.Rows.Clear();
        foreach (var acct in _config.Accounts)
        {
            var state = _guard.GetState(acct.Id);
            string status = !acct.Enabled ? Strings.StatusDisabled
                          : (state?.IsAlive == true) ? Strings.StatusAlive
                          : Strings.StatusDead;

            _grid.Rows.Add(
                acct.Id,
                acct.Enabled,
                string.IsNullOrEmpty(acct.Name) ? $"Account {acct.Id}" : acct.Name,
                acct.User,
                acct.Role,
                ResolveServerText(acct),
                acct.Mod,
                status);
        }
    }

    // ======== 事件处理 ========

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _config.Accounts.Count)
            return;

        var acct = _config.Accounts[e.RowIndex];
        switch (_grid.Columns[e.ColumnIndex].Name)
        {
            case "colEdit":
                EditAccount(acct);
                break;
            case "colLaunch":
                _ = LaunchSingleAsync(acct);
                break;
        }
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "colEnabled")
            return;

        _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_grid.IsCurrentCellDirty)
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _config.Accounts.Count)
            return;
        if (_grid.Columns[e.ColumnIndex].Name != "colEnabled")
            return;

        bool enabled = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells["colEnabled"].Value);
        var acct = _config.Accounts[e.RowIndex];
        if (acct.Enabled == enabled)
            return;

        acct.Enabled = enabled;
        SaveConfig();
        RefreshGrid();
    }

    // ======== 启动逻辑 ========

    private async Task LaunchAllAsync()
    {
        var enabledAccounts = _config.Accounts.Where(a => a.Enabled).ToList();
        if (enabledAccounts.Count == 0) return;

        // 检查启动前置条件
        if (!CheckLaunchPrerequisites()) return;

        _btnLaunchAll.Enabled = false;
        try
        {
            for (int i = 0; i < enabledAccounts.Count; i++)
            {
                var acct = enabledAccounts[i];

                // 第二个及之后的实例需要先清理互斥量
                if (i > 0)
                {
                    Log(string.Format(Strings.LogLaunching, acct.Id));
                    var (_, handleLog) = HandleCli.FindAndCloseAll(
                        _config.Global.HandleExePath, HandleCli.DefaultMutexName);
                    foreach (var line in handleLog) Log(line);
                }

                await LaunchSingleCoreAsync(acct);

                // 启动间隔
                if (i < enabledAccounts.Count - 1)
                {
                    _statusLabel.Text = $"Waiting {_config.Global.LaunchIntervalSec}s...";
                    await Task.Delay(_config.Global.LaunchIntervalSec * 1000);
                }
            }
        }
        finally
        {
            _btnLaunchAll.Enabled = true;
            _statusLabel.Text = "Ready";
            RefreshGrid();
        }
    }

    private async Task LaunchSingleAsync(AccountConfig acct)
    {
        if (!acct.Enabled) return;

        var state = _guard.GetState(acct.Id);
        if (state?.IsAlive == true)
        {
            Log($"[Info] Account {acct.Id} is already running / 账号 {acct.Id} 已在运行。");
            return;
        }

        // 不是第一个实例时清理互斥量
        if (_guard.GetAllStates().Any(s => s.IsAlive))
        {
            if (!CheckLaunchPrerequisites()) return;
            var (_, handleLog) = HandleCli.FindAndCloseAll(
                _config.Global.HandleExePath, HandleCli.DefaultMutexName);
            foreach (var line in handleLog) Log(line);
        }

        await LaunchSingleCoreAsync(acct);
        RefreshGrid();
    }

    private async Task LaunchSingleCoreAsync(AccountConfig acct)
    {
        EnsureAccountLayout(acct);
        acct.Layout.Borderless = false;

        Log(string.Format(Strings.LogLaunching, acct.Id));

        var presetPath = PresetManager.GetPresetPath(acct.Role);
        var result = Launcher.Launch(acct, _config.Global, presetPath);

        if (!result.Success)
        {
            Log(string.Format(Strings.LogFailed, acct.Id, result.Error));
            return;
        }

        Log(string.Format(Strings.LogLaunched, acct.Id, result.ProcessId));
        _guard.Register(acct.Id, result.ProcessId, result.ProcessHandle);

        // 异步等待窗口出现并排列
        Log(string.Format(Strings.LogArranging, acct.Id));
        bool arranged = await Task.Run(() =>
            WindowOps.ArrangeWindow(result.ProcessId, acct.Layout));

        if (arranged)
        {
            await Task.Delay(1500);
            await Task.Run(() => WindowOps.ArrangeWindow(result.ProcessId, acct.Layout, 3_000));
        }

        if (arranged)
        {
            Log(string.Format(Strings.LogArranged, acct.Id,
                acct.Layout.X, acct.Layout.Y, acct.Layout.W, acct.Layout.H));
        }
    }

    private void StopAll()
    {
        foreach (var state in _guard.GetAllStates())
        {
            try
            {
                var proc = Process.GetProcessById((int)state.ProcessId);
                if (!proc.HasExited) proc.Kill();
            }
            catch { /* 进程已退出 */ }
            _guard.Unregister(state.AccountId);
        }
        RefreshGrid();
    }

    // ======== handle.exe 检查 ========

    private bool CheckHandleExe()
    {
        ApplyQuickHandlePath();

        if (HandleCli.Exists(_config.Global.HandleExePath))
            return true;

        var result = MessageBox.Show(
            Strings.HandleNotFound + "\n\n" + Strings.HandleDownloadPrompt,
            Strings.HandleNotFoundTitle,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo(HandleCli.DownloadUrl) { UseShellExecute = true });
            }
            catch { /* 无法打开浏览器 */ }
        }

        return false;
    }

    // ======== 菜单功能 ========

    private void ShowGlobalSettings()
    {
        using var form = new GlobalSettingsForm(_config.Global);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _config.Global = form.Result;
            SaveConfig();
            SyncQuickSettingsFromConfig();
            RefreshGrid();
        }
    }

    private void ShowMonitorLayout()
    {
        using var form = new MonitorLayoutForm(_config);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _config = form.Result;
            SaveConfig();
            RefreshGrid();
        }
    }

    private void AddAccount()
    {
        int nextId = _config.Accounts.Count > 0
            ? _config.Accounts.Max(a => a.Id) + 1
            : 1;
        var newAccount = new AccountConfig
        {
            Id = nextId,
            Enabled = true,
            Name = $"Account {nextId}",
            Layout = CreateDefaultLayout(nextId)
        };

        using var form = new AccountEditorForm(newAccount, _config.Global);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _config.Accounts.Add(form.Result);
            SaveConfig();
            RefreshGrid();
        }
    }

    private void EditAccount(AccountConfig acct)
    {
        using var form = new AccountEditorForm(acct, _config.Global);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var idx = _config.Accounts.FindIndex(a => a.Id == acct.Id);
            if (idx >= 0) _config.Accounts[idx] = form.Result;
            SaveConfig();
            RefreshGrid();
        }
    }

    private void ImportConfig()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "JSON Files|*.json|All Files|*.*",
            Title = Strings.MenuImport
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var json = File.ReadAllText(ofd.FileName);
            _config = ConfigStore.Import(json);
            SaveConfig();
            RefreshGrid();
            Log("[Info] Config imported successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportConfig()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "JSON Files|*.json",
            Title = Strings.MenuExport,
            FileName = "d2r_multiplay_config.json"
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        var includePass = MessageBox.Show(
            Strings.ExportIncludePassword + "\n\n" + Strings.ExportPasswordWarning,
            Strings.Warning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

        var json = ConfigStore.Export(_config, includePass);
        File.WriteAllText(sfd.FileName, json);
        Log("[Info] Config exported.");
    }

    private void SwitchLanguage(string culture)
    {
        _config.Global.UiCulture = culture;
        SaveConfig();
        Program.ApplyCulture(culture);
        ApplyLocalization();
        RefreshGrid();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "D2R Multi-Play Manager v2.0\n\n" +
            "A compliant multi-instance launcher for Diablo II: Resurrected.\n" +
            "Uses Sysinternals handle.exe for mutex handling.\n\n" +
            "https://github.com/your-repo/multi_play_d2r",
            Strings.MenuAbout, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ======== 本地化 ========

    private void ApplyLocalization()
    {
        Text = Strings.AppTitle;
        _btnLaunchAll.Text = Strings.BtnLaunchAll;
        _btnStopAll.Text = Strings.BtnStopAll;
        _btnAddAccount.Text = Strings.BtnAddAccount;
        _btnLayout.Text = Strings.MenuLayout;
        _btnSettings.Text = Strings.MenuGlobalSettings;
        _btnLangZh.Text = "中文";
        _btnLangEn.Text = "English";
        _lnkHandleDownload.Text = "下载 handle.exe / Download";

        if (_grid.Columns.Count >= 10)
        {
            _grid.Columns["colId"].HeaderText = Strings.ColId;
            _grid.Columns["colEnabled"].HeaderText = Strings.ColEnabled;
            _grid.Columns["colName"].HeaderText = Strings.ColName;
            _grid.Columns["colUser"].HeaderText = Strings.ColEmail;
            _grid.Columns["colRole"].HeaderText = Strings.ColRole;
            _grid.Columns["colServer"].HeaderText = SServerHeader();
            _grid.Columns["colMod"].HeaderText = Strings.ColMod;
            _grid.Columns["colStatus"].HeaderText = Strings.ColStatus;
        }

        UpdateLanguageButtonState();
        UpdateD2rStatus();
        UpdateHandleStatus();

        // 重建菜单以应用新语言
        Controls.Remove(_menuStrip);
        _menuStrip.Dispose();
        SetupMenu();
    }

    // ======== 辅助 ========

    private void SaveConfig()
    {
        ConfigStore.Save(_config);
    }

    private void SyncQuickSettingsFromConfig()
    {
        if (_txtD2rPath != null)
            _txtD2rPath.Text = _config.Global.D2rExePath;
        if (_txtHandlePath != null)
            _txtHandlePath.Text = _config.Global.HandleExePath;
        UpdateD2rStatus();
        UpdateHandleStatus();
    }

    private void ApplyQuickD2rPath()
    {
        if (_txtD2rPath == null)
            return;

        var value = _txtD2rPath.Text.Trim();
        if (string.Equals(value, _config.Global.D2rExePath, StringComparison.Ordinal))
        {
            UpdateD2rStatus();
            return;
        }

        _config.Global.D2rExePath = value;
        SaveConfig();
        UpdateD2rStatus();
    }

    private void ApplyQuickHandlePath()
    {
        if (_txtHandlePath == null)
            return;

        var value = _txtHandlePath.Text.Trim();
        if (string.Equals(value, _config.Global.HandleExePath, StringComparison.Ordinal))
        {
            UpdateHandleStatus();
            return;
        }

        _config.Global.HandleExePath = value;
        SaveConfig();
        UpdateHandleStatus();
    }

    private void BrowseHandleExe()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "handle.exe|handle.exe|Executable|*.exe|All Files|*.*",
            Title = "Select handle.exe"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        _txtHandlePath.Text = ofd.FileName;
        ApplyQuickHandlePath();
    }

    private void BrowseD2rExe()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "D2R.exe|D2R.exe|Executable|*.exe|All Files|*.*",
            Title = "Select D2R.exe"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        _txtD2rPath.Text = ofd.FileName;
        ApplyQuickD2rPath();
    }

    private static void OpenHandleDownload()
    {
        try
        {
            Process.Start(new ProcessStartInfo(HandleCli.DownloadUrl) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    private void UpdateHandleStatus()
    {
        if (_lblHandleStatus == null)
            return;

        bool ok = HandleCli.Exists(_config.Global.HandleExePath);
        _lblHandleStatus.Text = ok ? "已配置 / OK" : "未配置(必填) / Required";
        _lblHandleStatus.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
        _txtHandlePath.BackColor = ok ? SystemColors.Window : Color.MistyRose;
    }

    private void UpdateD2rStatus()
    {
        if (_lblD2rStatus == null)
            return;

        bool ok = File.Exists(_config.Global.D2rExePath);
        _lblD2rStatus.Text = ok ? "已配置 / OK" : "未配置(必填) / Required";
        _lblD2rStatus.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
        _txtD2rPath.BackColor = ok ? SystemColors.Window : Color.MistyRose;
    }

    private bool CheckLaunchPrerequisites()
    {
        ApplyQuickD2rPath();
        ApplyQuickHandlePath();

        var missing = new List<string>();
        if (!File.Exists(_config.Global.D2rExePath))
            missing.Add("D2R.exe 路径");
        if (!HandleCli.Exists(_config.Global.HandleExePath))
            missing.Add("handle.exe 路径");

        UpdateD2rStatus();
        UpdateHandleStatus();

        if (missing.Count == 0)
            return true;

        MessageBox.Show(
            "以下前置条件未配置正确：\n- " + string.Join("\n- ", missing) + "\n\n请先在主界面补全后再启动。",
            Strings.Warning,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        return false;
    }

    private void UpdateLanguageButtonState()
    {
        bool zh = string.Equals(_config.Global.UiCulture, "zh-CN", StringComparison.OrdinalIgnoreCase);
        _btnLangZh.Font = new Font(_btnLangZh.Font, zh ? FontStyle.Bold : FontStyle.Regular);
        _btnLangEn.Font = new Font(_btnLangEn.Font, zh ? FontStyle.Regular : FontStyle.Bold);
    }

    private string ResolveServerText(AccountConfig acct)
    {
        return string.IsNullOrWhiteSpace(acct.ServerAddress)
            ? _config.Global.BattleNetAddress
            : acct.ServerAddress;
    }

    private WindowLayout CreateDefaultLayout(int accountId)
    {
        var monitors = MonitorEnumerator.Enumerate();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        if (primary == null)
            return new WindowLayout { W = 1280, H = 720, Borderless = false };

        int offset = ((accountId - 1) % 4) * 40;
        int width = Math.Min(1280, primary.WorkArea.Width);
        int height = Math.Min(720, primary.WorkArea.Height);

        return new WindowLayout
        {
            MonitorId = primary.DeviceName,
            X = primary.WorkArea.X + Math.Min(offset, Math.Max(0, primary.WorkArea.Width - width)),
            Y = primary.WorkArea.Y + Math.Min(offset, Math.Max(0, primary.WorkArea.Height - height)),
            W = width,
            H = height,
            Borderless = false
        };
    }

    private void EnsureAccountLayout(AccountConfig acct)
    {
        if (acct.Role.Equals("slave", StringComparison.OrdinalIgnoreCase))
        {
            acct.Layout.W = 1280;
            acct.Layout.H = 720;
        }

        if (acct.Layout.W <= 0)
            acct.Layout.W = 1280;
        if (acct.Layout.H <= 0)
            acct.Layout.H = 720;

        if (!string.IsNullOrWhiteSpace(acct.Layout.MonitorId))
            return;

        var fallback = CreateDefaultLayout(acct.Id);
        acct.Layout.MonitorId = fallback.MonitorId;
        acct.Layout.X = fallback.X;
        acct.Layout.Y = fallback.Y;
        acct.Layout.W = fallback.W;
        acct.Layout.H = fallback.H;
        acct.Layout.Borderless = false;
    }

    private static string SServerHeader() => "服务器 / Server";

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }
        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        _logBox.ScrollToCaret();
    }

    private void OnGuardStateChanged(InstanceStateChangedEventArgs e)
    {
        if (!e.State.IsAlive && e.WasAlive)
        {
            Log(string.Format(Strings.LogDied, e.State.AccountId));
        }
        RefreshGrid();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _guard.Stop();
        _guard.Dispose();
        base.OnFormClosing(e);
    }

    // 引用资源管理器包装类
    private static Resources.Strings Strings => new();
}
