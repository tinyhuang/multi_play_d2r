// ============================================================
// MainWindow.xaml.cs — 主窗口逻辑
// 账号管理、启动/停止、导入/导出、主题切换
// ============================================================

using System.Collections.ObjectModel;
using System.Windows.Documents;
using D2RMultiPlay.App.Resources;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AppConfig _config = null!;
    private ObservableCollection<AccountViewModel> _accounts = new();
    private static readonly Strings S = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (App.GlobalConfig is null)
        {
            MessageBox.Show("Failed to load configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        _config = App.GlobalConfig;
        DataGridAccounts.ItemsSource = _accounts;
        RefreshGrid();
        ApplyTheme();
        ApplyLocalization();
    }

    private void RefreshGrid()
    {
        _accounts.Clear();
        foreach (var acct in _config.Accounts)
        {
            _accounts.Add(new AccountViewModel(acct));
        }
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = new Run($"[{timestamp}] {message}\n") { Foreground = Brushes.LightGreen };
        RichTextBoxLog.Document.Blocks.Add(new Paragraph(line));
        RichTextBoxLog.ScrollToEnd();
    }

    private void SaveConfig()
    {
        ConfigStore.Save(_config);
    }

    // ===== File Menu =====

    private void MenuExportEncrypted_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new ExportPasswordDialog();
        if (dlg.ShowDialog(this) != true) return;

        var sfd = new SaveFileDialog
        {
            Filter = "D2R MultiPlay Config|*.d2rmp",
            FileName = "d2r_multiplay_config.d2rmp"
        };

        if (sfd.ShowDialog(this) != true) return;

        try
        {
            var encrypted = ConfigCrypto.Encrypt(_config, dlg.Passphrase, dlg.IncludePasswords);
            File.WriteAllText(sfd.FileName, encrypted, System.Text.Encoding.UTF8);
            Log("Configuration exported (encrypted .d2rmp).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, S.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuImportEncrypted_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "D2R MultiPlay Config|*.d2rmp|All Files|*.*"
        };

        if (ofd.ShowDialog(this) != true) return;

        using var dlg = new ImportPasswordDialog();
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            var envelopeJson = File.ReadAllText(ofd.FileName, System.Text.Encoding.UTF8);
            var imported = ConfigCrypto.Decrypt(envelopeJson, dlg.Passphrase);

            // Re-encrypt passwords with local DPAPI
            foreach (var acct in imported.Accounts)
            {
                if (!string.IsNullOrEmpty(acct.PassEnc))
                {
                    acct.PassEnc = ConfigStore.EncryptPassword(acct.PassEnc);
                }
            }

            _config = imported;
            SaveConfig();
            RefreshGrid();
            Log("Configuration imported from encrypted .d2rmp file.");
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            MessageBox.Show("Wrong passphrase or corrupted file.", S.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, S.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuExport_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Filter = "JSON Files|*.json",
            FileName = "d2r_multiplay_config.json"
        };

        if (sfd.ShowDialog(this) != true) return;

        var includePass = MessageBox.Show(
            S.ExportIncludePassword + "\n\n" + S.ExportPasswordWarning,
            S.Warning,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

        var json = ConfigStore.Export(_config, includePass);
        File.WriteAllText(sfd.FileName, json);
        Log("Configuration exported (JSON).");
    }

    private void MenuImport_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "JSON Files|*.json|All Files|*.*"
        };

        if (ofd.ShowDialog(this) != true) return;

        try
        {
            var json = File.ReadAllText(ofd.FileName);
            _config = ConfigStore.Import(json);
            SaveConfig();
            RefreshGrid();
            Log("Configuration imported (JSON).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, S.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ===== Accounts Menu =====

    private void MenuAddAccount_Click(object sender, RoutedEventArgs e)
    {
        BtnAddAccount_Click(null, null);
    }

    private void MenuLaunchAll_Click(object sender, RoutedEventArgs e)
    {
        BtnLaunchAll_Click(null, null);
    }

    private void MenuStopAll_Click(object sender, RoutedEventArgs e)
    {
        BtnStopAll_Click(null, null);
    }

    // ===== Tools Menu =====

    private void MenuGlobalSettings_Click(object sender, RoutedEventArgs e)
    {
        BtnSettings_Click(null, null);
    }

    private void MenuMonitorLayout_Click(object sender, RoutedEventArgs e)
    {
        BtnLayout_Click(null, null);
    }

    // ===== View Menu =====

    private void MenuLanguageChinese_Click(object sender, RoutedEventArgs e)
    {
        SwitchLanguage("zh-CN");
    }

    private void MenuLanguageEnglish_Click(object sender, RoutedEventArgs e)
    {
        SwitchLanguage("en-US");
    }

    private void MenuThemeDark_Click(object sender, RoutedEventArgs e)
    {
        SwitchTheme("dark");
    }

    private void MenuThemeLight_Click(object sender, RoutedEventArgs e)
    {
        SwitchTheme("light");
    }

    private void MenuToggleLog_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Toggle log panel visibility
    }

    // ===== Help Menu =====

    private void MenuQuickStart_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show quick start dialog
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show about dialog
    }

    // ===== Quick Action Buttons =====

    private void BtnLaunchAll_Click(object? sender, RoutedEventArgs? e)
    {
        // TODO: Implement launch all accounts
        Log("Launch All initiated (WIP)");
    }

    private void BtnStopAll_Click(object? sender, RoutedEventArgs? e)
    {
        // TODO: Implement stop all accounts
        Log("Stop All initiated (WIP)");
    }

    private void BtnAddAccount_Click(object? sender, RoutedEventArgs? e)
    {
        // TODO: Show account editor dialog
        Log("Add Account dialog (WIP)");
    }

    private void BtnSettings_Click(object? sender, RoutedEventArgs? e)
    {
        // TODO: Show global settings dialog
        Log("Global Settings dialog (WIP)");
    }

    private void BtnLayout_Click(object? sender, RoutedEventArgs? e)
    {
        // TODO: Show monitor layout dialog
        Log("Monitor Layout dialog (WIP)");
    }

    private void BtnLangZh_Click(object sender, RoutedEventArgs e)
    {
        SwitchLanguage("zh-CN");
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e)
    {
        SwitchLanguage("en-US");
    }

    // ===== Theme & Language =====

    private void SwitchLanguage(string culture)
    {
        _config.Global.UiCulture = culture;
        SaveConfig();
        App.ApplyCulture(culture);
        ApplyLocalization();
        RefreshGrid();
    }

    private void SwitchTheme(string theme)
    {
        _config.Global.UiTheme = theme;
        SaveConfig();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        bool dark = _config.Global.UiTheme == "dark";
        // TODO: Apply WPF-UI theme colors based on dark flag
    }

    private void ApplyLocalization()
    {
        Title = S.AppTitle;
        StatusLabel.Text = S.StatusReady;
        // TODO: Update all UI text from Strings resources
    }

    // ===== View Model for DataGrid =====

    public class AccountViewModel
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; } = "";
        public string User { get; set; } = "";
        public string Role { get; set; } = "";
        public string ServerAddress { get; set; } = "";
        public string Mod { get; set; } = "";
        public string StatusDisplay { get; set; } = "Offline";

        public AccountViewModel(AccountConfig cfg)
        {
            Id = cfg.Id;
            Enabled = cfg.Enabled;
            Name = cfg.Name;
            User = cfg.User;
            Role = cfg.Role;
            ServerAddress = cfg.ServerAddress;
            Mod = cfg.Mod;
            StatusDisplay = Enabled ? "Ready" : "Disabled";
        }
    }
}
