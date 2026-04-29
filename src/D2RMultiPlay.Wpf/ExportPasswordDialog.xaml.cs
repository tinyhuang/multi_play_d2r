// ============================================================
// ExportPasswordDialog.xaml.cs — 导出加密口令设置
// ============================================================

using System.Windows;
using D2RMultiPlay.Wpf.Resources;

namespace D2RMultiPlay.Wpf;

/// <summary>
/// Interaction logic for ExportPasswordDialog.xaml
/// </summary>
public partial class ExportPasswordDialog : Window
{
    private static readonly Strings S = new();

    public string Passphrase => PasswordBox1.Password;
    public bool IncludePasswords => ChkIncludePasswords.IsChecked ?? false;

    public ExportPasswordDialog()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var s = new Strings();
        Title = s.ExportSetPassphraseTitle;
        LblExportPassphrase.Text = s.ExportPassphraseLabel;
        LblExportPassphraseConfirm.Text = s.ExportPassphraseConfirmLabel;
        ChkIncludePasswords.Content = s.ExportIncludePasswordOption;
        LblExportWarning.Text = s.ExportPassphraseWarning;
        BtnCancelExport.Content = s.Cancel;
        BtnOkExport.Content = s.OK;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox1.Password.Length < 8)
        {
            MessageBox.Show("Passphrase must be at least 8 characters.", S.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordBox1.Focus();
            return;
        }

        if (PasswordBox1.Password != PasswordBox2.Password)
        {
            MessageBox.Show("Passphrases do not match. Please re-enter.", S.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordBox2.Focus();
            return;
        }

        DialogResult = true;
    }
}
