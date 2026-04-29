// ============================================================
// ImportPasswordDialog.xaml.cs — 导入加密口令输入
// ============================================================

using System.Windows;
using D2RMultiPlay.Wpf.Resources;

namespace D2RMultiPlay.Wpf;

/// <summary>
/// Interaction logic for ImportPasswordDialog.xaml
/// </summary>
public partial class ImportPasswordDialog : Window
{
    public string Passphrase => PasswordBox.Password;

    public ImportPasswordDialog()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var s = new Strings();
        Title = s.ImportEnterPassphraseTitle;
        LblImportPassphrase.Text = s.ImportPassphraseLabel;
        BtnCancelImport.Content = s.Cancel;
        BtnOkImport.Content = s.OK;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
