using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Monitors;

namespace D2RMultiPlay.Wpf;

public partial class MonitorLayoutDialog : Window
{
    public AppConfig Result { get; private set; }

    public MonitorLayoutDialog(AppConfig current)
    {
        InitializeComponent();
        
        var s = new Resources.Strings();
        Title = s.MonitorLayoutTitle;

        Result = ConfigStore.Import(ConfigStore.Export(current, includePasswords: true));

        EnsureLayouts();
        GridAccounts.ItemsSource = new ObservableCollection<AccountConfig>(Result.Accounts);
    }

    private void EnsureLayouts()
    {
        foreach (var acct in Result.Accounts)
        {
            acct.Layout ??= new WindowLayout { W = 1280, H = 720, Borderless = false };
            if (acct.Layout.W <= 0) acct.Layout.W = 1280;
            if (acct.Layout.H <= 0) acct.Layout.H = 720;
        }
    }

    private void BtnRefreshMonitors_Click(object sender, RoutedEventArgs e)
    {
        Result.Monitors = MonitorEnumerator.Enumerate()
            .Select(m => new MonitorConfig
            {
                Id = m.DeviceName,
                Bounds = [m.Bounds.X, m.Bounds.Y, m.Bounds.Width, m.Bounds.Height],
                WorkArea = [m.WorkArea.X, m.WorkArea.Y, m.WorkArea.Width, m.WorkArea.Height],
                DpiScale = m.DpiScale,
                IsPrimary = m.IsPrimary,
                RefreshHz = m.RefreshHz,
            })
            .ToList();

        MessageBox.Show($"Detected {Result.Monitors.Count} monitor(s).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnAutoGrid_Click(object sender, RoutedEventArgs e)
    {
        var monitors = MonitorEnumerator.Enumerate();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        if (primary == null)
            return;

        int cols = 2;
        int gap = 12;
        int cellW = (primary.WorkArea.Width - gap) / cols;
        int cellH = (int)(cellW * 9.0 / 16.0);

        for (int i = 0; i < Result.Accounts.Count; i++)
        {
            var acct = Result.Accounts[i];
            int row = i / cols;
            int col = i % cols;

            acct.Layout ??= new WindowLayout();
            acct.Layout.MonitorId = primary.DeviceName;
            acct.Layout.W = Math.Min(cellW, primary.WorkArea.Width);
            acct.Layout.H = Math.Min(cellH, primary.WorkArea.Height);
            acct.Layout.X = primary.WorkArea.X + col * (acct.Layout.W + gap);
            acct.Layout.Y = primary.WorkArea.Y + row * (acct.Layout.H + gap);
            acct.Layout.Borderless = false;
        }

        GridAccounts.ItemsSource = new ObservableCollection<AccountConfig>(Result.Accounts);
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Result.Version = Math.Max(1, Result.Version);
        DialogResult = true;
    }
}
