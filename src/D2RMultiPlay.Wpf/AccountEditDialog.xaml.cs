using System;
using System.Windows;
using Microsoft.Win32;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.Wpf;

public partial class AccountEditDialog : Window
{
    private readonly int _accountId;

    public AccountConfig Result { get; private set; } = new();

    public AccountEditDialog(int accountId, GlobalSettings global)
    {
        _accountId = accountId;
        InitializeComponent();

        TxtName.Text = $"Account {accountId}";
    }

    public AccountEditDialog(AccountConfig existing, GlobalSettings global)
    {
        _accountId = existing.Id;
        InitializeComponent();

        TxtName.Text = existing.Name;
        TxtUser.Text = existing.User;
        TxtPassword.Password = string.IsNullOrEmpty(existing.PassEnc) ? string.Empty : ConfigStore.DecryptPassword(existing.PassEnc);
        CmbRole.SelectedIndex = string.Equals(existing.Role, "slave", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        ChkEnabled.IsChecked = existing.Enabled;
        TxtIconPath.Text = existing.IconPath;
        TxtServer.Text = existing.ServerAddress;
    }

    private void BtnBrowseIcon_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Icon/Image|*.ico;*.png;*.jpg;*.jpeg;*.webp|All Files|*.*"
        };

        if (ofd.ShowDialog() == true)
            TxtIconPath.Text = ofd.FileName;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var role = ((ComboBoxItem)CmbRole.SelectedItem).Content?.ToString() ?? "master";

        Result = new AccountConfig
        {
            Id = _accountId,
            Enabled = ChkEnabled.IsChecked == true,
            Name = TxtName.Text.Trim(),
            User = TxtUser.Text.Trim(),
            PassEnc = string.IsNullOrEmpty(TxtPassword.Password) ? string.Empty : ConfigStore.EncryptPassword(TxtPassword.Password),
            Role = role,
            Mod = string.Empty,
            Options = string.Empty,
            ServerAddress = TxtServer.Text.Trim(),
            IconPath = TxtIconPath.Text.Trim(),
            ExePathOverride = string.Empty,
            Layout = new WindowLayout
            {
                W = 1280,
                H = 720,
                Borderless = false,
            }
        };

        DialogResult = true;
    }
}
