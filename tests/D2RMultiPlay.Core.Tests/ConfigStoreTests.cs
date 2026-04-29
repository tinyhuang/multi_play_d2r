// ============================================================
// ConfigStoreTests.cs — 配置读写与导入导出的单元测试
// ============================================================

using D2RMultiPlay.Core.Config;
using System.Runtime.Versioning;

namespace D2RMultiPlay.Core.Tests;

public class ConfigStoreTests
{
    [Fact]
    public void Load_NonExistentFile_ReturnsDefaultConfig()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"d2r_test_{Guid.NewGuid()}.json");
        try
        {
            var config = ConfigStore.Load(tempPath);

            Assert.NotNull(config);
            Assert.Equal(1, config.Version);
            Assert.NotNull(config.Global);
            Assert.Empty(config.Accounts);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"d2r_test_{Guid.NewGuid()}.json");
        try
        {
            var original = new AppConfig
            {
                Version = 1,
                Global = new GlobalSettings
                {
                    D2rExePath = @"C:\Games\D2R.exe",
                    HandleExePath = @"C:\Tools\handle.exe",
                    BattleNetAddress = "kr.actual.battle.net",
                    LaunchIntervalSec = 10,
                    MutexName = "Check For Other Instances",
                    UiCulture = "zh-CN"
                },
                Accounts =
                [
                    new AccountConfig
                    {
                        Id = 1, Enabled = true, Name = "Main",
                        IconPath = @"C:\Icons\main.png",
                        Role = "master", User = "test@example.com",
                        Mod = "tiny", Options = "-txt",
                        Layout = new WindowLayout { X = 0, Y = 0, W = 1920, H = 1080, Borderless = true }
                    }
                ]
            };

            ConfigStore.Save(original, tempPath);
            var loaded = ConfigStore.Load(tempPath);

            Assert.Equal(original.Version, loaded.Version);
            Assert.Equal(original.Global.D2rExePath, loaded.Global.D2rExePath);
            Assert.Equal(original.Global.BattleNetAddress, loaded.Global.BattleNetAddress);
            Assert.Equal(original.Global.LaunchIntervalSec, loaded.Global.LaunchIntervalSec);
            Assert.Single(loaded.Accounts);
            Assert.Equal("Main", loaded.Accounts[0].Name);
            Assert.Equal(@"C:\Icons\main.png", loaded.Accounts[0].IconPath);
            Assert.Equal("master", loaded.Accounts[0].Role);
            Assert.Equal(1920, loaded.Accounts[0].Layout.W);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void Export_WithoutPasswords_StripsPassEnc()
    {
        var config = new AppConfig
        {
            Accounts =
            [
                new AccountConfig { Id = 1, PassEnc = "SomeEncryptedData", User = "test@test.com" }
            ]
        };

        var json = ConfigStore.Export(config, includePasswords: false);
        var reimported = ConfigStore.Import(json);

        Assert.Equal("", reimported.Accounts[0].PassEnc);
        Assert.Equal("test@test.com", reimported.Accounts[0].User);
    }

    [Fact]
    public void Export_WithPasswords_RetainsPassEnc()
    {
        var config = new AppConfig
        {
            Accounts =
            [
                new AccountConfig { Id = 1, PassEnc = "SomeEncryptedData" }
            ]
        };

        var json = ConfigStore.Export(config, includePasswords: true);
        var reimported = ConfigStore.Import(json);

        Assert.Equal("SomeEncryptedData", reimported.Accounts[0].PassEnc);
    }

    [Fact]
    public void Import_InvalidJson_ThrowsException()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => ConfigStore.Import("not valid json"));
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void ExportPortable_WithoutPasswords_ClearsPasswordField()
    {
        var config = new AppConfig
        {
            Accounts =
            [
                new AccountConfig { Id = 1, User = "test@test.com", PassEnc = "EncryptedPlaceholder" }
            ]
        };

        var json = ConfigStore.ExportPortable(config, includePasswords: false);
        var reimported = ConfigStore.Import(json);

        Assert.Equal(string.Empty, reimported.Accounts[0].PassEnc);
        Assert.Equal("test@test.com", reimported.Accounts[0].User);
    }
}
