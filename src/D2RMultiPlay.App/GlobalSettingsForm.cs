// ============================================================
// GlobalSettingsForm.cs — 全局设置对话框
// ============================================================

using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.App;

public sealed class GlobalSettingsForm : Form
{
    public GlobalSettings Result { get; private set; }

    private TextBox _txtD2rExe = null!;
    private TextBox _txtHandleExe = null!;
    private TextBox _txtServer = null!;
    private NumericUpDown _nudInterval = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    private static Resources.Strings S => new();

    public GlobalSettingsForm(GlobalSettings settings)
    {
        Result = settings;
        BuildUI();
        LoadData(settings);
    }

    private void BuildUI()
    {
        Text = S.GlobalSettingsTitle;
        Size = new Size(560, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35));

        int row = 0;

        // D2R.exe
        table.Controls.Add(new Label { Text = S.LblD2rExe, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtD2rExe = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtD2rExe, 1, row);
        var btnBrowseD2r = new Button { Text = "...", Width = 30 };
        btnBrowseD2r.Click += (_, _) => BrowseFile(_txtD2rExe, "D2R.exe|D2R.exe|Executable|*.exe");
        table.Controls.Add(btnBrowseD2r, 2, row);
        row++;

        // handle.exe
        table.Controls.Add(new Label { Text = S.LblHandleExe, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtHandleExe = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtHandleExe, 1, row);
        var btnBrowseHandle = new Button { Text = "...", Width = 30 };
        btnBrowseHandle.Click += (_, _) => BrowseFile(_txtHandleExe, "handle.exe|handle.exe|Executable|*.exe");
        table.Controls.Add(btnBrowseHandle, 2, row);
        row++;

        // 服务器
        table.Controls.Add(new Label { Text = S.LblServer, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _txtServer = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(_txtServer, 1, row);
        table.SetColumnSpan(_txtServer, 2);
        row++;

        var hintLabel = new Label
        {
            Text = "账号留空时会使用这里的默认服务器。",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 0, 0, 8)
        };
        table.Controls.Add(hintLabel, 1, row);
        table.SetColumnSpan(hintLabel, 2);
        row++;

        // 启动间隔
        table.Controls.Add(new Label { Text = S.LblInterval, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        _nudInterval = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1, Maximum = 60, Value = 8,
            DecimalPlaces = 0
        };
        table.Controls.Add(_nudInterval, 1, row);
        table.SetColumnSpan(_nudInterval, 2);
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

    private void LoadData(GlobalSettings s)
    {
        _txtD2rExe.Text = s.D2rExePath;
        _txtHandleExe.Text = s.HandleExePath;
        _txtServer.Text = s.BattleNetAddress;
        _nudInterval.Value = Math.Clamp(s.LaunchIntervalSec, 1, 60);
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        Result = new GlobalSettings
        {
            D2rExePath = _txtD2rExe.Text.Trim(),
            HandleExePath = _txtHandleExe.Text.Trim(),
            BattleNetAddress = _txtServer.Text.Trim(),
            LaunchIntervalSec = (int)_nudInterval.Value,
            MutexName = Result.MutexName,
            ProfilesRoot = Result.ProfilesRoot,
            SlaveAffinityMask = Result.SlaveAffinityMask,
            UiCulture = Result.UiCulture
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void BrowseFile(TextBox target, string filter)
    {
        using var ofd = new OpenFileDialog { Filter = filter };
        if (ofd.ShowDialog() == DialogResult.OK)
            target.Text = ofd.FileName;
    }
}
