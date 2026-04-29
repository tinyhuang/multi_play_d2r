using D2RMultiPlay.Core.Config;
using System.Runtime.Versioning;

namespace D2RMultiPlay.Core.Tests;

public class ConfigCryptoTests
{
    [Fact]
    [SupportedOSPlatform("windows")]
    public void EncryptDecrypt_WithoutPasswords_RoundTrip_Succeeds()
    {
        var config = new AppConfig
        {
            Global = new GlobalSettings
            {
                D2rExePath = @"C:\Games\D2R.exe",
                HandleExePath = @"C:\Tools\handle.exe",
                BattleNetAddress = "kr.actual.battle.net",
                UiCulture = "en-US",
                UiTheme = "dark",
                IconStyle = "gamer",
            },
            Accounts =
            [
                new AccountConfig
                {
                    Id = 1,
                    Enabled = true,
                    Name = "Main",
                    IconPath = @"C:\Icons\main.png",
                    Role = "master",
                    User = "demo@example.com",
                    PassEnc = "EncryptedPlaceholder",
                }
            ]
        };

        var passphrase = "correct horse battery staple";
        var encrypted = ConfigCrypto.Encrypt(config, passphrase, includePasswords: false);
        var decrypted = ConfigCrypto.Decrypt(encrypted, passphrase);

        Assert.Equal(config.Global.D2rExePath, decrypted.Global.D2rExePath);
        Assert.Equal(config.Global.HandleExePath, decrypted.Global.HandleExePath);
        Assert.Equal(config.Accounts[0].Name, decrypted.Accounts[0].Name);
        Assert.Equal(config.Accounts[0].IconPath, decrypted.Accounts[0].IconPath);

        // includePasswords=false should strip password payload in portable export
        Assert.Equal(string.Empty, decrypted.Accounts[0].PassEnc);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void Decrypt_WithWrongPassphrase_ThrowsCryptographicException()
    {
        var config = new AppConfig
        {
            Accounts = [ new AccountConfig { Id = 1, Name = "Main" } ]
        };

        var encrypted = ConfigCrypto.Encrypt(config, "right-passphrase", includePasswords: false);

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(
            () => ConfigCrypto.Decrypt(encrypted, "wrong-passphrase"));
    }
}
