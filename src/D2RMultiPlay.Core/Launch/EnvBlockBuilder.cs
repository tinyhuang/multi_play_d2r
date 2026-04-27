// ============================================================
// EnvBlockBuilder.cs — 构造带自定义 USERPROFILE 的环境块
// 用于 CreateProcessW 的 lpEnvironment 参数
// ============================================================

using System.Runtime.InteropServices;
using System.Text;

namespace D2RMultiPlay.Core.Launch;

public static class EnvBlockBuilder
{
    /// <summary>
    /// 基于当前进程的环境变量，覆盖 USERPROFILE 后构造 Unicode 环境块
    /// 返回 GCHandle-pinned 的非托管指针（调用方负责 Free）
    /// </summary>
    /// <param name="fakeProfile">账号伪 profile 路径（如 %APPDATA%\D2RMultiPlay\profiles\account_1）</param>
    /// <returns>(环境块指针, GCHandle) — 用完后必须 handle.Free()</returns>
    public static (IntPtr Pointer, GCHandle PinnedHandle) Build(string fakeProfile)
    {
        // 复制当前进程环境变量
        var env = Environment.GetEnvironmentVariables();
        var dict = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry entry in env)
        {
            dict[entry.Key?.ToString() ?? ""] = entry.Value?.ToString() ?? "";
        }

        // 覆盖 USERPROFILE 为账号隔离路径
        dict["USERPROFILE"] = fakeProfile;

        // 构造 Unicode 环境块格式：KEY=VALUE\0KEY=VALUE\0...\0\0
        var sb = new StringBuilder();
        foreach (var kv in dict)
        {
            sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\0');
        }
        sb.Append('\0'); // 终止双 null

        var bytes = Encoding.Unicode.GetBytes(sb.ToString());
        var gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        return (gcHandle.AddrOfPinnedObject(), gcHandle);
    }

    /// <summary>
    /// 确保账号 profile 目录及 Saved Games 子目录存在；
    /// 首次创建时从全局 Settings.json 拷贝种子配置
    /// </summary>
    /// <param name="fakeProfile">账号伪 profile 根路径</param>
    /// <param name="presetSettingsPath">可选：预设 Settings.json 路径（master/slave 模板）</param>
    public static void EnsureProfileDir(string fakeProfile, string? presetSettingsPath = null)
    {
        // D2R 的存档路径: USERPROFILE\Saved Games\Diablo II Resurrected
        var d2rSaveDir = Path.Combine(fakeProfile, "Saved Games", "Diablo II Resurrected");
        var settingsFile = Path.Combine(d2rSaveDir, "Settings.json");

        if (Directory.Exists(d2rSaveDir) && File.Exists(settingsFile))
            return; // 已经初始化过

        Directory.CreateDirectory(d2rSaveDir);

        // 优先使用预设模板
        if (!string.IsNullOrEmpty(presetSettingsPath) && File.Exists(presetSettingsPath))
        {
            File.Copy(presetSettingsPath, settingsFile, overwrite: false);
            return;
        }

        // 其次从系统全局 Settings.json 复制种子
        var globalSettings = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Saved Games", "Diablo II Resurrected", "Settings.json");

        if (File.Exists(globalSettings))
        {
            File.Copy(globalSettings, settingsFile, overwrite: false);
        }
        // 如果全局也没有，D2R 首次启动会自动生成
    }
}
