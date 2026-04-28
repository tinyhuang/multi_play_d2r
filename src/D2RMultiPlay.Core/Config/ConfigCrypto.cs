// ============================================================
// ConfigCrypto.cs — 跨机器配置加密（AES-256-GCM + PBKDF2）
// 用于导出/导入 .d2rmp 文件，使账号信息在传输中不泄漏
// ============================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D2RMultiPlay.Core.Config;

/// <summary>
/// 加密信封 JSON 结构（.d2rmp 文件顶层）
/// </summary>
public sealed class EncryptedEnvelope
{
    /// <summary>信封格式版本</summary>
    public int EnvelopeVersion { get; set; } = 1;

    /// <summary>PBKDF2 盐（Base64）</summary>
    public string Salt { get; set; } = "";

    /// <summary>AES-GCM Nonce（Base64）</summary>
    public string Nonce { get; set; } = "";

    /// <summary>AES-GCM 认证标签（Base64）</summary>
    public string Tag { get; set; } = "";

    /// <summary>密文（Base64）</summary>
    public string Ciphertext { get; set; } = "";

    /// <summary>是否包含密码</summary>
    public bool IncludesPasswords { get; set; }
}

[JsonSerializable(typeof(EncryptedEnvelope))]
internal partial class EnvelopeJsonContext : JsonSerializerContext { }

public static class ConfigCrypto
{
    private const int SaltBytes = 16;
    private const int NonceBytes = 12;   // AES-GCM 标准 nonce 长度
    private const int TagBytes = 16;     // 128-bit 认证标签
    private const int KeyBytes = 32;     // AES-256
    private const int Pbkdf2Iterations = 200_000;

    /// <summary>
    /// 将 AppConfig 加密为 .d2rmp 信封 JSON
    /// </summary>
    /// <param name="config">要导出的配置</param>
    /// <param name="passphrase">用户设置的口令</param>
    /// <param name="includePasswords">是否包含账号密码</param>
    /// <returns>可直接写入文件的 JSON 字符串</returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static string Encrypt(AppConfig config, string passphrase, bool includePasswords)
    {
        // 先用 ConfigStore.ExportPortable 获取可移植明文 JSON（密码已从 DPAPI 解密）
        var plainJson = ConfigStore.ExportPortable(config, includePasswords);
        var plainBytes = Encoding.UTF8.GetBytes(plainJson);

        // 派生密钥
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            KeyBytes);

        // AES-256-GCM 加密
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(key, TagBytes);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // 构建信封
        var envelope = new EncryptedEnvelope
        {
            EnvelopeVersion = 1,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Ciphertext = Convert.ToBase64String(ciphertext),
            IncludesPasswords = includePasswords,
        };

        return JsonSerializer.Serialize(envelope, EnvelopeJsonContext.Default.EncryptedEnvelope);
    }

    /// <summary>
    /// 从 .d2rmp 信封 JSON 解密还原 AppConfig
    /// </summary>
    /// <param name="envelopeJson">文件内容</param>
    /// <param name="passphrase">用户输入的口令</param>
    /// <returns>解密后的配置</returns>
    public static AppConfig Decrypt(string envelopeJson, string passphrase)
    {
        var envelope = JsonSerializer.Deserialize(envelopeJson, EnvelopeJsonContext.Default.EncryptedEnvelope)
                       ?? throw new InvalidOperationException("Invalid .d2rmp file format.");

        if (envelope.EnvelopeVersion != 1)
            throw new NotSupportedException($"Unsupported envelope version: {envelope.EnvelopeVersion}");

        var salt = Convert.FromBase64String(envelope.Salt);
        var nonce = Convert.FromBase64String(envelope.Nonce);
        var tag = Convert.FromBase64String(envelope.Tag);
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);

        // 派生密钥
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            KeyBytes);

        // AES-256-GCM 解密
        var plainBytes = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagBytes);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes);

        var plainJson = Encoding.UTF8.GetString(plainBytes);
        return ConfigStore.Import(plainJson);
    }
}
