// ============================================================
// AccountEditorForm.cs — 账号编辑对话框
// ============================================================

using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Launch;

namespace D2RMultiPlay.App;

public sealed class AccountEditorForm : Form
{
    private readonly AccountConfig _original;
    private readonly GlobalSettings _global;
    public AccountConfig Result { get; private set; }

    private TextBox _txtName = null!;
    private TextBox _txtUser = null!;
    private TextBox _txtPassword = null!;
    private ComboBox _cboRole = null!;
    private ComboBox _cboServer = null!;
    private TextBox _txtMod = null!;
    private TextBox _txtOptions = null!;
    private TextBox _txtExePath = null!;
    private NumericUpDown _nudWidth = null!;
    private NumericUpDown _nudHeight = null!;
    private TextBox _txtPreview = null!;
    private CheckBox _chkEnabled = null!;
    private Button _btnBrowse = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    private static Resources.Strings S => new();

    public AccountEditorForm(AccountConfig account, GlobalSettings global)
    {
        _original = account;
        _global = global;
        Result = account;
        BuildUI();
        LoadData();
        UpdatePreview();
    }

    private void BuildUI()
    {
        Text = S.AccountEditorTitle;
        Size = new Size(760, 680);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(12),
            AutoScroll = true
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35));

        int row = 0;

        // 启用
        _chkEnabled = new CheckBox { Text = S.ChkEnabled, AutoSize = true };
        table.Controls.Add(_chkEnabled, 0, row);
        table.SetColumnSpan(_chkEnabled, 3);
        row++;

        // 名称
        table.Controls.Add(new Label { Text = S.LblName, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtName = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtName, 1, row);
        table.SetColumnSpan(_txtName, 2);
        row++;

        // 邮箱
        table.Controls.Add(new Label { Text = S.LblUser, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtUser = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtUser, 1, row);
        table.SetColumnSpan(_txtUser, 2);
        row++;

        // 密码
        table.Controls.Add(new Label { Text = S.LblPassword, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        table.Controls.Add(_txtPassword, 1, row);
        table.SetColumnSpan(_txtPassword, 2);
        row++;

        // 角色
        table.Controls.Add(new Label { Text = S.LblRole, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _cboRole = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboRole.Items.AddRange([S.RoleMaster, S.RoleSlave]);
        table.Controls.Add(_cboRole, 1, row);
        table.SetColumnSpan(_cboRole, 2);
        row++;

        // 服务器（账号级覆盖）
        table.Controls.Add(new Label { Text = S.LblServerOverride, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _cboServer = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboServer.Items.Add(S.ServerUseGlobalDefault);
        _cboServer.Items.AddRange(BattleNetServers.All);
        table.Controls.Add(_cboServer, 1, row);
        table.SetColumnSpan(_cboServer, 2);
        row++;

        // Mod
        table.Controls.Add(new Label { Text = S.LblMod, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtMod = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtMod, 1, row);
        table.SetColumnSpan(_txtMod, 2);
        row++;

        // 附加参数
        table.Controls.Add(new Label { Text = S.LblOptions, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtOptions = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtOptions, 1, row);
        table.SetColumnSpan(_txtOptions, 2);
        row++;

        // 独立路径
        table.Controls.Add(new Label { Text = S.LblExePath, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtExePath = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtExePath, 1, row);
        _btnBrowse = new Button { Text = "...", Width = 30 };
        _btnBrowse.Click += (_, _) => BrowseExe(_txtExePath);
        table.Controls.Add(_btnBrowse, 2, row);
        row++;

        // 窗口宽度
        table.Controls.Add(new Label { Text = S.LblWindowWidth, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _nudWidth = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Minimum = 640,
            Maximum = 7680,
            Increment = 10,
            Width = 140
        };
        table.Controls.Add(_nudWidth, 1, row);
        table.SetColumnSpan(_nudWidth, 2);
        row++;

        // 窗口高度
        table.Controls.Add(new Label { Text = S.LblWindowHeight, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _nudHeight = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Minimum = 360,
            Maximum = 4320,
            Increment = 10,
            Width = 140
        };
        table.Controls.Add(_nudHeight, 1, row);
        table.SetColumnSpan(_nudHeight, 2);
        row++;

        // 启动预览
        table.Controls.Add(new Label
        {
            Text = S.LblLaunchPreview,
            Anchor = AnchorStyles.Left,
            AutoSize = true
        }, 0, row);
        _txtPreview = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 120,
            Font = new Font("Consolas", 9F),
            WordWrap = false
        };
        table.Controls.Add(_txtPreview, 1, row);
        table.SetColumnSpan(_txtPreview, 2);
        row++;

        // 按钮
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        _btnCancel = new Button { Text = S.Cancel, DialogResult = DialogResult.Cancel, Width = 80 };
        _btnOk = new Button { Text = S.OK, Width = 80 };
        _btnOk.Click += BtnOk_Click;
        btnPanel.Controls.AddRange([_btnCancel, _btnOk]);

        root.Controls.Add(table, 0, 0);
        root.Controls.Add(btnPanel, 0, 1);
        Controls.Add(root);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _txtName.TextChanged += (_, _) => UpdatePreview();
        _txtUser.TextChanged += (_, _) => UpdatePreview();
        _txtPassword.TextChanged += (_, _) => UpdatePreview();
        _cboRole.SelectedIndexChanged += (_, _) =>
        {
            if (_cboRole.SelectedIndex == 1)
            {
                _nudWidth.Value = 1280;
                _nudHeight.Value = 720;
            }
            UpdatePreview();
        };
        _cboServer.SelectedIndexChanged += (_, _) => UpdatePreview();
        _txtMod.TextChanged += (_, _) => UpdatePreview();
        _txtOptions.TextChanged += (_, _) => UpdatePreview();
        _txtExePath.TextChanged += (_, _) => UpdatePreview();
        _nudWidth.ValueChanged += (_, _) => UpdatePreview();
        _nudHeight.ValueChanged += (_, _) => UpdatePreview();
    }

    private void LoadData()
    {
        _chkEnabled.Checked = _original.Enabled;
        _txtName.Text = _original.Name;
        _txtUser.Text = _original.User;
        _cboRole.SelectedIndex = _original.Role.Equals("slave", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (string.IsNullOrWhiteSpace(_original.ServerAddress))
        {
            _cboServer.SelectedIndex = 0;
        }
        else
        {
            var server = _original.ServerAddress.Trim();
            if (!_cboServer.Items.Contains(server))
                _cboServer.Items.Add(server);
            _cboServer.SelectedItem = server;
        }
        _txtMod.Text = _original.Mod;
        _txtOptions.Text = _original.Options;
        _txtExePath.Text = _original.ExePathOverride;
        _nudWidth.Value = Math.Clamp(_original.Layout.W > 0 ? _original.Layout.W : 1280, 640, 7680);
        _nudHeight.Value = Math.Clamp(_original.Layout.H > 0 ? _original.Layout.H : 720, 360, 4320);

        // 密码字段：如果已有加密密码则显示占位符
        if (!string.IsNullOrEmpty(_original.PassEnc))
            _txtPassword.PlaceholderText = "••••••••";
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        // 密码处理：只有用户输入了新密码才重新加密
        string passEnc = _original.PassEnc;
        if (!string.IsNullOrEmpty(_txtPassword.Text))
        {
            passEnc = ConfigStore.EncryptPassword(_txtPassword.Text);
        }

        Result = new AccountConfig
        {
            Id = _original.Id,
            Enabled = _chkEnabled.Checked,
            Name = _txtName.Text.Trim(),
            User = _txtUser.Text.Trim(),
            PassEnc = passEnc,
            Role = _cboRole.SelectedIndex == 1 ? "slave" : "master",
            ServerAddress = ResolveSelectedServer(),
            Mod = _txtMod.Text.Trim(),
            Options = _txtOptions.Text.Trim(),
            ExePathOverride = _txtExePath.Text.Trim(),
            Layout = new WindowLayout
            {
                MonitorId = _original.Layout.MonitorId,
                X = _original.Layout.X,
                Y = _original.Layout.Y,
                W = (int)_nudWidth.Value,
                H = (int)_nudHeight.Value,
                Borderless = false
            }
        };

        if (Result.Role.Equals("slave", StringComparison.OrdinalIgnoreCase))
        {
            Result.Layout.W = 1280;
            Result.Layout.H = 720;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdatePreview()
    {
        if (_txtPreview == null)
            return;

        var previewAccount = new AccountConfig
        {
            Id = _original.Id,
            Enabled = _chkEnabled.Checked,
            Name = _txtName.Text.Trim(),
            User = _txtUser.Text.Trim(),
            PassEnc = _original.PassEnc,
            Role = _cboRole.SelectedIndex == 1 ? "slave" : "master",
            ServerAddress = ResolveSelectedServer(),
            Mod = _txtMod.Text.Trim(),
            Options = _txtOptions.Text.Trim(),
            ExePathOverride = _txtExePath.Text.Trim(),
            Layout = new WindowLayout
            {
                MonitorId = _original.Layout.MonitorId,
                X = _original.Layout.X,
                Y = _original.Layout.Y,
                W = (int)_nudWidth.Value,
                H = (int)_nudHeight.Value,
                Borderless = false
            }
        };

        if (previewAccount.Role.Equals("slave", StringComparison.OrdinalIgnoreCase))
        {
            previewAccount.Layout.W = 1280;
            previewAccount.Layout.H = 720;
        }

        string? passwordOverride = string.IsNullOrWhiteSpace(_txtPassword.Text)
            ? null
            : _txtPassword.Text;

        _txtPreview.Text = Launcher.BuildPreviewCommandLine(previewAccount, _global, passwordOverride)
            + Environment.NewLine + Environment.NewLine
            + S.PreviewLayoutHint;
    }

    private string ResolveSelectedServer()
    {
        if (_cboServer.SelectedIndex <= 0)
            return string.Empty;

        return _cboServer.Text.Trim();
    }

    private static void BrowseExe(TextBox target)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Executable|*.exe|All Files|*.*",
            Title = S.SelectD2rExeTitle
        };
        if (ofd.ShowDialog() == DialogResult.OK)
            target.Text = ofd.FileName;
    }
}
