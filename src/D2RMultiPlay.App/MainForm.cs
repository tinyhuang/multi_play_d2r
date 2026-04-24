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
            ReadOnly = true,
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

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "colId", HeaderText = Strings.ColId, Width = 40, FillWeight = 8 },
            new DataGridViewCheckBoxColumn { Name = "colEnabled", HeaderText = "✓", Width = 30, FillWeight = 5 },
            new DataGridViewTextBoxColumn { Name = "colName", HeaderText = Strings.ColName, FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "colRole", HeaderText = Strings.ColRole, Width = 80, FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "colMod", HeaderText = Strings.ColMod, Width = 80, FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = Strings.ColStatus, Width = 80, FillWeight = 12 },
            new DataGridViewButtonColumn { Name = "colEdit", HeaderText = "", Text = Strings.BtnEdit, UseColumnTextForButtonValue = true, Width = 60, FillWeight = 8 },
            new DataGridViewButtonColumn { Name = "colLaunch", HeaderText = "", Text = Strings.BtnLaunch, UseColumnTextForButtonValue = true, Width = 60, FillWeight = 8 },
            new DataGridViewButtonColumn { Name = "colReconnect", HeaderText = "", Text = Strings.BtnReconnect, UseColumnTextForButtonValue = true, Width = 80, FillWeight = 10 }
        );

        _grid.CellClick += Grid_CellClick;
        _splitContainer.Panel1.Controls.Add(_grid);
    }

    private void SetupButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4)
        };

        _btnLaunchAll = new Button { Text = Strings.BtnLaunchAll, Width = 100 };
        _btnLaunchAll.Click += async (_, _) => await LaunchAllAsync();

        _btnStopAll = new Button { Text = Strings.BtnStopAll, Width = 100 };
        _btnStopAll.Click += (_, _) => StopAll();

        _btnAddAccount = new Button { Text = Strings.BtnAddAccount, Width = 100 };
        _btnAddAccount.Click += (_, _) => AddAccount();

        panel.Controls.AddRange([_btnLaunchAll, _btnStopAll, _btnAddAccount]);
        _splitContainer.Panel1.Controls.Add(panel);
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
                string.IsNullOrEmpty(acct.Name) ? acct.User : acct.Name,
                acct.Role,
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
            case "colReconnect":
                _ = ReconnectAsync(acct);
                break;
        }
    }

    // ======== 启动逻辑 ========

    private async Task LaunchAllAsync()
    {
        var enabledAccounts = _config.Accounts.Where(a => a.Enabled).ToList();
        if (enabledAccounts.Count == 0) return;

        // 检查 handle.exe
        if (!CheckHandleExe()) return;

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
                        _config.Global.HandleExePath, _config.Global.MutexName);
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

        // 不是第一个实例时清理互斥量
        if (_guard.GetAllStates().Any(s => s.IsAlive))
        {
            if (!CheckHandleExe()) return;
            var (_, handleLog) = HandleCli.FindAndCloseAll(
                _config.Global.HandleExePath, _config.Global.MutexName);
            foreach (var line in handleLog) Log(line);
        }

        await LaunchSingleCoreAsync(acct);
        RefreshGrid();
    }

    private async Task LaunchSingleCoreAsync(AccountConfig acct)
    {
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
            Log(string.Format(Strings.LogArranged, acct.Id,
                acct.Layout.X, acct.Layout.Y, acct.Layout.W, acct.Layout.H));
        }
    }

    private async Task ReconnectAsync(AccountConfig acct)
    {
        var state = _guard.GetState(acct.Id);
        if (state?.IsAlive == true) return; // 还活着就不重连

        _guard.Unregister(acct.Id);
        await LaunchSingleAsync(acct);
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
        var newAccount = new AccountConfig { Id = nextId, Enabled = true, Name = $"Account {nextId}" };

        using var form = new AccountEditorForm(newAccount);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _config.Accounts.Add(form.Result);
            SaveConfig();
            RefreshGrid();
        }
    }

    private void EditAccount(AccountConfig acct)
    {
        using var form = new AccountEditorForm(acct);
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

        if (_grid.Columns.Count >= 9)
        {
            _grid.Columns["colId"].HeaderText = Strings.ColId;
            _grid.Columns["colName"].HeaderText = Strings.ColName;
            _grid.Columns["colRole"].HeaderText = Strings.ColRole;
            _grid.Columns["colMod"].HeaderText = Strings.ColMod;
            _grid.Columns["colStatus"].HeaderText = Strings.ColStatus;
        }

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
