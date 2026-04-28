// ============================================================
// ImportPasswordDialog.xaml.cs — 导入加密口令输入
// ============================================================

using System.Windows;

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
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
