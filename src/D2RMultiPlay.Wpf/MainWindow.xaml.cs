// ============================================================
// MainWindow.xaml.cs — 主窗口逻辑
// 账号管理、启动/停止、导入/导出、主题切换
// ============================================================

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Guard;
using D2RMultiPlay.Core.Handles;
using D2RMultiPlay.Core.Launch;
using D2RMultiPlay.Core.Monitors;
using D2RMultiPlay.Core.Windows;
using D2RMultiPlay.Wpf.Resources;

namespace D2RMultiPlay.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AppConfig _config = null!;
    private readonly ObservableCollection<AccountViewModel> _accounts = new();
    private readonly ProcessGuard _guard = new();
    private bool _logVisible = true;
    private static readonly Strings S = new();

    public MainWindow()
    {
        InitializeComponent();
        _guard.StateChanged += Guard_StateChanged;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
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
        _guard.Start();
        RefreshGrid();
        ApplyTheme();
        ApplyLocalization();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _guard.Dispose();
    }

    private void Guard_StateChanged(object? sender, InstanceStateChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.WasAlive && !e.State.IsAlive)
            {
                Log(string.Format(S.LogDied, e.State.AccountId));
            }
            RefreshGrid();
        });
    }

    private void RefreshGrid()
    {
        _accounts.Clear();
        var states = _guard.GetAllStates();
        foreach (var acct in _config.Accounts)
        {
            var state = states.FirstOrDefault(s => s.AccountId == acct.Id);
            _accounts.Add(new AccountViewModel(acct, state?.IsAlive == true));
        }

        BtnStopAll.IsEnabled = states.Any(s => s.IsAlive);
        BtnLaunchAll.IsEnabled = _config.Accounts.Any(a => a.Enabled && !(states.FirstOrDefault(s => s.AccountId == a.Id)?.IsAlive ?? false));
        UpdateSelectionActions();
    }

    private void UpdateSelectionActions()
    {
        var acct = GetSelectedAccount();
        if (acct == null)
        {
            BtnEditSelected.IsEnabled = false;
            BtnDeleteSelected.IsEnabled = false;
            BtnLaunchSelected.IsEnabled = false;
            BtnStopSelected.IsEnabled = false;
            return;
        }

        var state = _guard.GetState(acct.Id);
        var isAlive = state?.IsAlive == true;

        BtnEditSelected.IsEnabled = true;
        BtnDeleteSelected.IsEnabled = true;
        BtnLaunchSelected.IsEnabled = acct.Enabled && !isAlive;
        BtnStopSelected.IsEnabled = isAlive;
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
        var dlg = new ExportPasswordDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var sfd = new SaveFileDialog
        {
            Filter = "D2R MultiPlay Config|*.d2rmp",
            FileName = "d2r_multiplay_config.d2rmp"
        };

        if (sfd.ShowDialog() != true) return;

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

        if (ofd.ShowDialog() != true) return;

        var dlg = new ImportPasswordDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;

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

        if (sfd.ShowDialog() != true) return;

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

        if (ofd.ShowDialog() != true) return;

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
        var grid = (Grid)RichTextBoxLog.Parent;
        _logVisible = !_logVisible;
        RichTextBoxLog.Visibility = _logVisible ? Visibility.Visible : Visibility.Collapsed;
        grid.RowDefinitions[2].Height = _logVisible ? new GridLength(200) : new GridLength(0);
    }

    // ===== Help Menu =====

    private void MenuQuickStart_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(S.QuickStartContent, S.MenuQuickStart, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(S.AboutContent, S.MenuAbout, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ===== Quick Action Buttons =====

    private async void BtnLaunchAll_Click(object? sender, RoutedEventArgs? e)
    {
        await LaunchAllAsync();
    }

    private void BtnStopAll_Click(object? sender, RoutedEventArgs? e)
    {
        StopAll();
    }

    private void BtnAddAccount_Click(object? sender, RoutedEventArgs? e)
    {
        int nextId = _config.Accounts.Count > 0 ? _config.Accounts.Max(a => a.Id) + 1 : 1;
        var dlg = new AccountEditDialog(nextId, _config.Global) { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        var created = dlg.Result;
        created.Layout = CreateDefaultLayout(created.Id);
        _config.Accounts.Add(created);
        SaveConfig();
        RefreshGrid();
        Log($"Account {created.Id} added.");
    }

    private void BtnEditSelected_Click(object sender, RoutedEventArgs e)
    {
        EditSelectedAccount();
    }

    private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var acct = GetSelectedAccount();
        if (acct == null)
            return;

        var confirm = MessageBox.Show(
            $"Delete account #{acct.Id} ({acct.Name})?",
            S.Warning,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        StopSingle(acct.Id);
        _config.Accounts.RemoveAll(a => a.Id == acct.Id);
        SaveConfig();
        RefreshGrid();
        Log($"Account {acct.Id} deleted.");
    }

    private async void BtnLaunchSelected_Click(object sender, RoutedEventArgs e)
    {
        var acct = GetSelectedAccount();
        if (acct == null)
            return;

        if (!CheckLaunchPrerequisites())
            return;

        if (_guard.GetAllStates().Any(s => s.IsAlive))
        {
            var (_, handleLog) = HandleCli.FindAndCloseAll(_config.Global.HandleExePath, HandleCli.DefaultMutexName);
            foreach (var line in handleLog)
                Log(line);
        }

        await LaunchSingleCoreAsync(acct);
        RefreshGrid();
    }

    private void BtnStopSelected_Click(object sender, RoutedEventArgs e)
    {
        var acct = GetSelectedAccount();
        if (acct == null)
            return;

        StopSingle(acct.Id);
    }

    private void BtnSettings_Click(object? sender, RoutedEventArgs? e)
    {
        var dlg = new GlobalSettingsDialog(_config.Global) { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        _config.Global = dlg.Result;
        SaveConfig();
        ApplyTheme();
        ApplyLocalization();
        RefreshGrid();
        Log("Global settings updated.");
    }

    private void BtnLayout_Click(object? sender, RoutedEventArgs? e)
    {
        var dlg = new MonitorLayoutDialog(_config) { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        _config = dlg.Result;
        SaveConfig();
        RefreshGrid();
        Log("Monitor layout updated.");
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
        bool dark = string.Equals(_config.Global.UiTheme, "dark", StringComparison.OrdinalIgnoreCase);
        Background = dark ? new SolidColorBrush(Color.FromRgb(30, 33, 43)) : SystemColors.ControlBrush;
    }

    private void ApplyLocalization()
    {
        Title = S.AppTitle;

        MenuFileRoot.Header = S.MenuFile;
        MenuExportEncrypted.Header = S.MenuExport + " (.d2rmp)";
        MenuImportEncrypted.Header = S.MenuImport + " (.d2rmp)";
        MenuExportPlain.Header = S.MenuExport + " (JSON)";
        MenuImportPlain.Header = S.MenuImport + " (JSON)";
        MenuExit.Header = S.MenuExit;

        MenuAccountsRoot.Header = S.MenuAccounts;
        MenuAddAccount.Header = S.BtnAddAccount;
        MenuLaunchAll.Header = S.BtnLaunchAll;
        MenuStopAll.Header = S.BtnStopAll;

        MenuToolsRoot.Header = S.MenuTools;
        MenuGlobalSettings.Header = S.MenuGlobalSettings;
        MenuMonitorLayout.Header = S.MenuLayout;

        MenuViewRoot.Header = S.MenuView;
        MenuLanguageRoot.Header = S.MenuLanguage;
        MenuThemeRoot.Header = S.MenuTheme;
        MenuThemeDark.Header = S.MenuThemeDark;
        MenuThemeLight.Header = S.MenuThemeLight;
        MenuToggleLog.Header = S.MenuToggleLog;

        MenuHelpRoot.Header = S.MenuHelp;
        MenuQuickStart.Header = S.MenuQuickStart;
        MenuAbout.Header = S.MenuAbout;

        BtnLaunchAll.Content = S.BtnLaunchAll;
        BtnStopAll.Content = S.BtnStopAll;
        BtnAddAccount.Content = S.BtnAddAccount;
        BtnEditSelected.Content = S.BtnEdit;
        BtnDeleteSelected.Content = S.BtnDelete;
        BtnLaunchSelected.Content = S.BtnLaunch;
        BtnStopSelected.Content = S.BtnStop;
        BtnLayout.Content = S.MenuLayout;
        BtnSettings.Content = S.MenuGlobalSettings;

        ColIcon.Header = "Icon";
        ColId.Header = S.ColId;
        ColEnabled.Header = S.ColEnabled;
        ColName.Header = S.ColName;
        ColEmail.Header = S.ColEmail;
        ColRole.Header = S.ColRole;
        ColServer.Header = S.ColServer;
        ColMod.Header = S.ColMod;
        ColStatus.Header = S.ColStatus;

        StatusLabel.Text = S.StatusReady;
    }

    private bool CheckLaunchPrerequisites()
    {
        var missing = new List<string>();
        if (!File.Exists(_config.Global.D2rExePath))
            missing.Add(S.MissingD2rPath);
        if (!HandleCli.Exists(_config.Global.HandleExePath))
            missing.Add(S.MissingHandlePath);

        if (missing.Count == 0)
            return true;

        MessageBox.Show(
            string.Format(S.LaunchPrereqMessage, string.Join("\n- ", missing)),
            S.Warning,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private async Task LaunchAllAsync()
    {
        var enabledAccounts = _config.Accounts.Where(a => a.Enabled).ToList();
        if (enabledAccounts.Count == 0)
            return;

        if (!CheckLaunchPrerequisites())
            return;

        BtnLaunchAll.IsEnabled = false;
        try
        {
            for (int i = 0; i < enabledAccounts.Count; i++)
            {
                var acct = enabledAccounts[i];

                if (i > 0)
                {
                    var (_, handleLog) = HandleCli.FindAndCloseAll(
                        _config.Global.HandleExePath, HandleCli.DefaultMutexName);
                    foreach (var line in handleLog)
                        Log(line);
                }

                await LaunchSingleCoreAsync(acct);

                if (i < enabledAccounts.Count - 1)
                    await Task.Delay(_config.Global.LaunchIntervalSec * 1000);
            }
        }
        finally
        {
            BtnLaunchAll.IsEnabled = true;
            RefreshGrid();
        }
    }

    private async Task LaunchSingleCoreAsync(AccountConfig acct)
    {
        EnsureAccountLayout(acct);
        acct.Layout.Borderless = false;

        Log(string.Format(S.LogLaunching, acct.Id));

        var presetPath = PresetManager.GetPresetPath(acct.Role);
        var result = Launcher.Launch(acct, _config.Global, presetPath);

        if (!result.Success)
        {
            Log(string.Format(S.LogFailed, acct.Id, result.Error));
            return;
        }

        Log(string.Format(S.LogLaunched, acct.Id, result.ProcessId));
        _guard.Register(acct.Id, result.ProcessId, result.ProcessHandle);

        Log(string.Format(S.LogArranging, acct.Id));
        bool arranged = await Task.Run(() => WindowOps.ArrangeWindow(result.ProcessId, acct.Layout));
        if (arranged)
        {
            await Task.Delay(1500);
            _ = await Task.Run(() => WindowOps.ArrangeWindow(result.ProcessId, acct.Layout, 3000));
            Log(string.Format(S.LogArranged, acct.Id, acct.Layout.X, acct.Layout.Y, acct.Layout.W, acct.Layout.H));
        }
    }

    private void StopAll()
    {
        foreach (var state in _guard.GetAllStates())
        {
            try
            {
                var proc = Process.GetProcessById((int)state.ProcessId);
                if (!proc.HasExited)
                    proc.Kill();
            }
            catch
            {
                // ignore already-exited process
            }

            _guard.Unregister(state.AccountId);
        }

        RefreshGrid();
    }

    private void StopSingle(int accountId)
    {
        var state = _guard.GetState(accountId);
        if (state == null || !state.IsAlive)
            return;

        try
        {
            var proc = Process.GetProcessById((int)state.ProcessId);
            if (!proc.HasExited)
                proc.Kill();
        }
        catch
        {
            // ignore already-exited process
        }

        _guard.Unregister(accountId);
        RefreshGrid();
    }

    private AccountConfig? GetSelectedAccount()
    {
        if (DataGridAccounts.SelectedItem is not AccountViewModel vm)
            return null;

        return _config.Accounts.FirstOrDefault(a => a.Id == vm.Id);
    }

    private void EditSelectedAccount()
    {
        var acct = GetSelectedAccount();
        if (acct == null)
            return;

        var dlg = new AccountEditDialog(acct, _config.Global) { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        var idx = _config.Accounts.FindIndex(a => a.Id == acct.Id);
        if (idx >= 0)
        {
            var updated = dlg.Result;
            updated.Layout = acct.Layout;
            _config.Accounts[idx] = updated;
            SaveConfig();
            RefreshGrid();
            Log($"Account {updated.Id} updated.");
        }
    }

    private void DataGridAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectionActions();
    }

    private WindowLayout CreateDefaultLayout(int accountId)
    {
        var monitors = MonitorEnumerator.Enumerate();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        if (primary == null)
            return new WindowLayout { W = 1280, H = 720, Borderless = false };

        int offset = ((accountId - 1) % 4) * 40;
        int width = Math.Min(1280, primary.WorkArea.Width);
        int height = Math.Min(720, primary.WorkArea.Height);

        return new WindowLayout
        {
            MonitorId = primary.DeviceName,
            X = primary.WorkArea.X + Math.Min(offset, Math.Max(0, primary.WorkArea.Width - width)),
            Y = primary.WorkArea.Y + Math.Min(offset, Math.Max(0, primary.WorkArea.Height - height)),
            W = width,
            H = height,
            Borderless = false,
        };
    }

    private void EnsureAccountLayout(AccountConfig acct)
    {
        if (acct.Layout == null)
            acct.Layout = new WindowLayout();

        if (acct.Role.Equals("slave", StringComparison.OrdinalIgnoreCase))
        {
            acct.Layout.W = 1280;
            acct.Layout.H = 720;
        }

        if (acct.Layout.W <= 0)
            acct.Layout.W = 1280;
        if (acct.Layout.H <= 0)
            acct.Layout.H = 720;

        if (!string.IsNullOrWhiteSpace(acct.Layout.MonitorId))
            return;

        var fallback = CreateDefaultLayout(acct.Id);
        acct.Layout.MonitorId = fallback.MonitorId;
        acct.Layout.X = fallback.X;
        acct.Layout.Y = fallback.Y;
        acct.Layout.W = fallback.W;
        acct.Layout.H = fallback.H;
        acct.Layout.Borderless = false;
    }

    private void DataGridAccounts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        EditSelectedAccount();
    }

    // ===== View Model for DataGrid =====

    public class AccountViewModel
    {
        public ImageSource? IconImage { get; set; }
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; } = "";
        public string User { get; set; } = "";
        public string Role { get; set; } = "";
        public string ServerAddress { get; set; } = "";
        public string Mod { get; set; } = "";
        public string StatusDisplay { get; set; } = "Offline";

        public AccountViewModel(AccountConfig cfg, bool isAlive)
        {
            IconImage = CreateIconImage(cfg.IconPath);
            Id = cfg.Id;
            Enabled = cfg.Enabled;
            Name = cfg.Name;
            User = cfg.User;
            Role = cfg.Role;
            ServerAddress = cfg.ServerAddress;
            Mod = cfg.Mod;
            StatusDisplay = !Enabled ? S.StatusDisabled : (isAlive ? S.StatusAlive : S.StatusDead);
        }

        private static ImageSource? CreateIconImage(string? iconPath)
        {
            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(iconPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 24;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch
                {
                    // fallback to default icon below
                }
            }

            var fallback = new DrawingImage(new GeometryDrawing(
                Brushes.Goldenrod,
                null,
                Geometry.Parse("M3,4 L21,4 21,20 3,20 Z M6,8 L18,8 18,16 6,16 Z")));
            fallback.Freeze();
            return fallback;
        }
    }
}
