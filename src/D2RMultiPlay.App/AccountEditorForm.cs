// ============================================================
// AccountEditorForm.cs — 账号编辑对话框
// ============================================================

using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.App;

public sealed class AccountEditorForm : Form
{
    private readonly AccountConfig _original;
    public AccountConfig Result { get; private set; }

    private TextBox _txtName = null!;
    private TextBox _txtUser = null!;
    private TextBox _txtPassword = null!;
    private ComboBox _cboRole = null!;
    private TextBox _txtMod = null!;
    private TextBox _txtOptions = null!;
    private TextBox _txtExePath = null!;
    private CheckBox _chkEnabled = null!;
    private Button _btnBrowse = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    private static Resources.Strings S => new();

    public AccountEditorForm(AccountConfig account)
    {
        _original = account;
        Result = account;
        BuildUI();
        LoadData();
    }

    private void BuildUI()
    {
        Text = S.AccountEditorTitle;
        Size = new Size(480, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

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
        _chkEnabled = new CheckBox { Text = "Enabled / 启用", AutoSize = true };
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

        // 按钮
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        _btnCancel = new Button { Text = S.Cancel, DialogResult = DialogResult.Cancel, Width = 80 };
        _btnOk = new Button { Text = S.OK, Width = 80 };
        _btnOk.Click += BtnOk_Click;
        btnPanel.Controls.AddRange([_btnCancel, _btnOk]);

        Controls.Add(table);
        Controls.Add(btnPanel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private void LoadData()
    {
        _chkEnabled.Checked = _original.Enabled;
        _txtName.Text = _original.Name;
        _txtUser.Text = _original.User;
        _cboRole.SelectedIndex = _original.Role.Equals("slave", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _txtMod.Text = _original.Mod;
        _txtOptions.Text = _original.Options;
        _txtExePath.Text = _original.ExePathOverride;

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
            Mod = _txtMod.Text.Trim(),
            Options = _txtOptions.Text.Trim(),
            ExePathOverride = _txtExePath.Text.Trim(),
            Layout = _original.Layout // 保留现有布局
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void BrowseExe(TextBox target)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Executable|*.exe|All Files|*.*",
            Title = "Select D2R.exe"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
            target.Text = ofd.FileName;
    }
}
