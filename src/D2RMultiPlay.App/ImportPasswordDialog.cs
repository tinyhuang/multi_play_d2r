// ============================================================
// ImportPasswordDialog.cs — 导入加密口令输入对话框
// ============================================================

using D2RMultiPlay.App.Resources;

namespace D2RMultiPlay.App;

internal sealed class ImportPasswordDialog : Form
{
    private readonly TextBox _txtPass;

    public string Passphrase => _txtPass.Text;

    private static readonly Strings S = new();

    public ImportPasswordDialog()
    {
        Text = S.ImportEnterPassphraseTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(400, 180);
        Padding = new Padding(20);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
        };

        layout.Controls.Add(new Label { Text = S.ImportPassphraseLabel, AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        _txtPass = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        layout.Controls.Add(_txtPass);

        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
        var btnCancel = new Button { Text = S.Cancel, DialogResult = DialogResult.Cancel, Width = 80 };
        var btnOk = new Button { Text = S.OK, DialogResult = DialogResult.OK, Width = 80 };
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);
        layout.Controls.Add(btnPanel);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
