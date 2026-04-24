// ============================================================
// PresetManager.cs — D2R Settings.json 预设模板管理
// 提供 master（高画质）和 slave（低画质/静音）两套预设
// ============================================================

using System.Text.Json;

namespace D2RMultiPlay.Core.Launch;

public static class PresetManager
{
    /// <summary>预设模板存储目录</summary>
    public static string PresetsDir =>
        Path.Combine(Config.ConfigStore.DefaultConfigDir, "presets");

    /// <summary>Master 预设路径</summary>
    public static string MasterPresetPath => Path.Combine(PresetsDir, "master.json");

    /// <summary>Slave 预设路径</summary>
    public static string SlavePresetPath => Path.Combine(PresetsDir, "slave.json");

    /// <summary>
    /// 确保预设目录和模板文件存在
    /// </summary>
    public static void EnsurePresets()
    {
        if (!Directory.Exists(PresetsDir))
            Directory.CreateDirectory(PresetsDir);

        if (!File.Exists(MasterPresetPath))
            File.WriteAllText(MasterPresetPath, MasterSettingsJson);

        if (!File.Exists(SlavePresetPath))
            File.WriteAllText(SlavePresetPath, SlaveSettingsJson);
    }

    /// <summary>
    /// 获取指定角色的预设路径
    /// </summary>
    public static string? GetPresetPath(string role)
    {
        EnsurePresets();
        return role.Equals("slave", StringComparison.OrdinalIgnoreCase)
            ? SlavePresetPath
            : MasterPresetPath;
    }

    /// <summary>
    /// 将某个账号的 Settings.json 替换为预设模板
    /// </summary>
    public static void ApplyPreset(string profileRoot, int accountId, string role)
    {
        var presetPath = GetPresetPath(role);
        if (presetPath == null || !File.Exists(presetPath))
            return;

        var d2rSaveDir = Path.Combine(profileRoot, $"account_{accountId}",
            "Saved Games", "Diablo II Resurrected");
        var settingsFile = Path.Combine(d2rSaveDir, "Settings.json");

        Directory.CreateDirectory(d2rSaveDir);
        File.Copy(presetPath, settingsFile, overwrite: true);
    }

    // ---- 预设 JSON 内容 ----

    // Master 预设：高画质（D2R 默认值，仅设置窗口模式和常见优化项）
    private const string MasterSettingsJson = """
    {
        "Window Mode": 0,
        "Resolution Width": 1920,
        "Resolution Height": 1080,
        "Sharpening": 6,
        "Graphics Quality": 3,
        "Shadow Quality": 3,
        "Ambient Occlusion Quality": 1,
        "Anti Aliasing": 1,
        "VSync": 0,
        "Master Volume": 50,
        "Music Volume": 30,
        "Ambient Sound Volume": 50
    }
    """;

    // Slave 预设：最低画质、静音、最小化资源占用
    private const string SlaveSettingsJson = """
    {
        "Window Mode": 0,
        "Resolution Width": 1280,
        "Resolution Height": 720,
        "Sharpening": 0,
        "Graphics Quality": 0,
        "Shadow Quality": 0,
        "Ambient Occlusion Quality": 0,
        "Anti Aliasing": 0,
        "VSync": 1,
        "Master Volume": 0,
        "Music Volume": 0,
        "Ambient Sound Volume": 0
    }
    """;
}
