// ============================================================
// ConfigStore.cs — 配置文件读写 + DPAPI 密码加解密
// 持久化路径: %APPDATA%\D2RMultiPlay\config.json
// ============================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace D2RMultiPlay.Core.Config;

public static class ConfigStore
{
    /// <summary>默认配置目录</summary>
    public static string DefaultConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "D2RMultiPlay");

    /// <summary>默认配置文件路径</summary>
    public static string DefaultConfigPath =>
        Path.Combine(DefaultConfigDir, "config.json");

    /// <summary>默认 profiles 根目录</summary>
    public static string DefaultProfilesRoot =>
        Path.Combine(DefaultConfigDir, "profiles");

    // ---- 读写 ----

    /// <summary>
    /// 从磁盘加载配置；文件不存在则返回默认配置
    /// </summary>
    public static AppConfig Load(string? path = null)
    {
        path ??= DefaultConfigPath;
        if (!File.Exists(path))
        {
            var cfg = new AppConfig();
            cfg.Global.ProfilesRoot = DefaultProfilesRoot;
            return cfg;
        }

        var json = File.ReadAllText(path, Encoding.UTF8);
        var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
        return config ?? new AppConfig();
    }

    /// <summary>
    /// 将配置写入磁盘（原子写：先写临时文件再 rename）
    /// </summary>
    public static void Save(AppConfig config, string? path = null)
    {
        path ??= DefaultConfigPath;
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json, Encoding.UTF8);
        // 原子替换，防止写入中断导致配置损坏
        File.Move(tmp, path, overwrite: true);
    }

    // ---- DPAPI 密码加解密（仅 Windows） ----

    /// <summary>
    /// 用 DPAPI (CurrentUser) 加密明文密码，返回 Base64 字符串
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static string EncryptPassword(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return "";

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// 用 DPAPI (CurrentUser) 解密 Base64 密文，返回明文
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static string DecryptPassword(string base64Cipher)
    {
        if (string.IsNullOrEmpty(base64Cipher))
            return "";

        var encrypted = Convert.FromBase64String(base64Cipher);
        var plainBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    // ---- 导入/导出 ----

    /// <summary>
    /// 导出配置（默认剥离密码字段）
    /// </summary>
    public static string Export(AppConfig config, bool includePasswords = false)
    {
        // 深拷贝：序列化再反序列化
        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var clone = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig)!;

        if (!includePasswords)
        {
            foreach (var acct in clone.Accounts)
                acct.PassEnc = "";
        }

        return JsonSerializer.Serialize(clone, AppConfigJsonContext.Default.AppConfig);
    }

    /// <summary>
    /// 从 JSON 字符串导入配置
    /// </summary>
    public static AppConfig Import(string json)
    {
        return JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig)
               ?? throw new InvalidOperationException("Invalid config JSON");
    }
}
