// ============================================================
// ExportPasswordDialog.cs — 导出加密口令设置对话框
// ============================================================

using D2RMultiPlay.App.Resources;

namespace D2RMultiPlay.App;

internal sealed class ExportPasswordDialog : Form
{
    private readonly TextBox _txtPass1;
    private readonly TextBox _txtPass2;
    private readonly CheckBox _chkIncludePasswords;
    private readonly Label _lblWarning;

    public string Passphrase => _txtPass1.Text;
    public bool IncludePasswords => _chkIncludePasswords.Checked;

    private static readonly Strings S = new();

    public ExportPasswordDialog()
    {
        Text = S.ExportSetPassphraseTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(420, 320);
        Padding = new Padding(20);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            AutoSize = true,
        };

        // 口令输入
        layout.Controls.Add(new Label { Text = S.ExportPassphraseLabel, AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        _txtPass1 = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        layout.Controls.Add(_txtPass1);

        layout.Controls.Add(new Label { Text = S.ExportPassphraseConfirmLabel, AutoSize = true, Margin = new Padding(0, 8, 0, 4) });
        _txtPass2 = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        layout.Controls.Add(_txtPass2);

        // 包含密码
        _chkIncludePasswords = new CheckBox
        {
            Text = S.ExportIncludePasswordOption,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 4),
            Checked = true,
        };
        layout.Controls.Add(_chkIncludePasswords);

        // 警告
        _lblWarning = new Label
        {
            Text = S.ExportPassphraseWarning,
            AutoSize = true,
            ForeColor = Color.OrangeRed,
            Font = new Font(Font.FontFamily, 8.5f),
            Margin = new Padding(0, 4, 0, 8),
        };
        layout.Controls.Add(_lblWarning);

        // 按钮
        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        var btnCancel = new Button { Text = S.Cancel, DialogResult = DialogResult.Cancel, Width = 80 };
        var btnOk = new Button { Text = S.OK, Width = 80 };
        btnOk.Click += BtnOk_Click;
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);
        layout.Controls.Add(btnPanel);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (_txtPass1.Text.Length < 8)
        {
            MessageBox.Show(this, S.ExportPassphraseTooShort, S.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPass1.Focus();
            return;
        }
        if (_txtPass1.Text != _txtPass2.Text)
        {
            MessageBox.Show(this, S.ExportPassphraseMismatch, S.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPass2.Focus();
            return;
        }
        DialogResult = DialogResult.OK;
    }
}
