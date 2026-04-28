using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.Wpf;

public partial class GlobalSettingsDialog : Window
{
    public GlobalSettings Result { get; private set; } = new();

    public GlobalSettingsDialog(GlobalSettings current)
    {
        InitializeComponent();

        TxtD2r.Text = current.D2rExePath;
        TxtHandle.Text = current.HandleExePath;
        TxtServer.Text = current.BattleNetAddress;
        TxtInterval.Text = current.LaunchIntervalSec.ToString();
        TxtProfilesRoot.Text = current.ProfilesRoot;

        SelectComboItem(CmbTheme, current.UiTheme);
        SelectComboItem(CmbIconStyle, current.IconStyle);
        SelectComboItem(CmbCulture, current.UiCulture);
    }

    private static void SelectComboItem(ComboBox combo, string value)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item &&
                string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void BtnBrowseD2r_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog { Filter = "Executable|*.exe|All Files|*.*" };
        if (ofd.ShowDialog() == true)
            TxtD2r.Text = ofd.FileName;
    }

    private void BtnBrowseHandle_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog { Filter = "Executable|*.exe|All Files|*.*" };
        if (ofd.ShowDialog() == true)
            TxtHandle.Text = ofd.FileName;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtInterval.Text.Trim(), out var interval) || interval <= 0)
        {
            MessageBox.Show("Launch interval must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var theme = (CmbTheme.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "dark";
        var iconStyle = (CmbIconStyle.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "gamer";
        var culture = (CmbCulture.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "zh-CN";

        Result = new GlobalSettings
        {
            D2rExePath = TxtD2r.Text.Trim(),
            HandleExePath = TxtHandle.Text.Trim(),
            BattleNetAddress = TxtServer.Text.Trim(),
            LaunchIntervalSec = interval,
            ProfilesRoot = TxtProfilesRoot.Text.Trim(),
            MutexName = "Check For Other Instances",
            SlaveAffinityMask = 0,
            UiCulture = culture,
            UiTheme = theme,
            IconStyle = iconStyle,
        };

        DialogResult = true;
    }
}
