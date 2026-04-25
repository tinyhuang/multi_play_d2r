// ============================================================
// Launcher.cs — D2R 进程启动器
// 使用 CreateProcessW 注入独立 USERPROFILE 环境变量
// ============================================================

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using D2RMultiPlay.Core.Config;
using D2RMultiPlay.Core.Interop;

namespace D2RMultiPlay.Core.Launch;

/// <summary>
/// 启动结果
/// </summary>
public sealed class LaunchResult
{
    public bool Success { get; init; }
    public uint ProcessId { get; init; }
    public IntPtr ProcessHandle { get; init; }
    public string Error { get; init; } = "";
}

[SupportedOSPlatform("windows")]
public static class Launcher
{
    /// <summary>
    /// D2R 启动参数白名单（2026 版本，已移除 -legacy）
    /// </summary>
    private static readonly HashSet<string> KnownParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "-username", "-password", "-address", "-mod",
        "-txt", "-ns", "-lq", "-direct", "-uiasset", "-w"
    };

    /// <summary>
    /// 启动一个 D2R 实例
    /// </summary>
    /// <param name="account">账号配置</param>
    /// <param name="global">全局设置</param>
    /// <param name="presetPath">可选的 Settings.json 预设模板路径</param>
    public static LaunchResult Launch(AccountConfig account, GlobalSettings global, string? presetPath = null)
    {
        // 解析游戏路径：账号独立路径 > 全局路径
        var exePath = ResolveExePath(account, global);

        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return new LaunchResult
            {
                Success = false,
                Error = $"游戏路径不存在 / Game executable not found: {exePath}"
            };
        }

        // 确定 profile 隔离路径
        var profilesRoot = !string.IsNullOrWhiteSpace(global.ProfilesRoot)
            ? global.ProfilesRoot
            : ConfigStore.DefaultProfilesRoot;
        var fakeProfile = Path.Combine(profilesRoot, $"account_{account.Id}");

        // 初始化 profile 目录（首次拷贝 Settings.json 种子）
        EnvBlockBuilder.EnsureProfileDir(fakeProfile, presetPath);

        // 构造命令行
        var cmdLine = BuildCommandLine(exePath, account, global, passwordOverride: null, maskStoredPassword: false);

        // 构造环境块（注入 USERPROFILE）
        var (envPtr, envHandle) = EnvBlockBuilder.Build(fakeProfile);

        try
        {
            var si = new WinStructs.STARTUPINFO { cb = Marshal.SizeOf<WinStructs.STARTUPINFO>() };

            bool created = NativeMethods.CreateProcess(
                null,                                           // lpApplicationName
                cmdLine,                                        // lpCommandLine
                IntPtr.Zero,                                    // lpProcessAttributes
                IntPtr.Zero,                                    // lpThreadAttributes
                false,                                          // bInheritHandles
                WinConst.CREATE_UNICODE_ENVIRONMENT,            // 关键：告知系统环境块是 Unicode
                envPtr,                                         // lpEnvironment（隔离的 USERPROFILE）
                Path.GetDirectoryName(exePath),                 // lpCurrentDirectory
                ref si,
                out var pi);

            if (!created)
            {
                int err = Marshal.GetLastWin32Error();
                return new LaunchResult
                {
                    Success = false,
                    Error = $"CreateProcess 失败 / CreateProcess failed (Win32 error {err})"
                };
            }

            // 关闭线程句柄（不需要），保留进程句柄给调用方
            NativeMethods.CloseHandle(pi.hThread);

            // 挂机窗口降低优先级
            if (account.Role.Equals("slave", StringComparison.OrdinalIgnoreCase))
            {
                NativeMethods.SetPriorityClass(pi.hProcess, WinConst.BELOW_NORMAL_PRIORITY_CLASS);
            }

            return new LaunchResult
            {
                Success = true,
                ProcessId = pi.dwProcessId,
                ProcessHandle = pi.hProcess
            };
        }
        finally
        {
            envHandle.Free(); // 释放 pinned 环境块
        }
    }

    /// <summary>
    /// 构造用于 UI 展示的预览命令行
    /// </summary>
    public static string BuildPreviewCommandLine(AccountConfig account, GlobalSettings global, string? passwordOverride = null)
    {
        var exePath = ResolveExePath(account, global);
        return BuildCommandLine(exePath, account, global, passwordOverride, maskStoredPassword: true);
    }

    /// <summary>
    /// 构造 D2R 启动命令行
    /// </summary>
    private static string BuildCommandLine(
        string exePath,
        AccountConfig account,
        GlobalSettings global,
        string? passwordOverride,
        bool maskStoredPassword)
    {
        var sb = new StringBuilder();
        sb.Append('"').Append(exePath).Append('"');
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 基础参数
        sb.Append(" -w"); // 窗口模式（强制）
        flags.Add("-w");

        // 账号凭据
        if (!string.IsNullOrEmpty(account.User))
            AppendSwitch(sb, "-username", account.User);

        if (!string.IsNullOrEmpty(passwordOverride))
        {
            AppendSwitch(sb, "-password", passwordOverride);
        }
        else if (!string.IsNullOrEmpty(account.PassEnc))
        {
            var pass = ConfigStore.DecryptPassword(account.PassEnc);
            if (!string.IsNullOrEmpty(pass))
            {
                AppendSwitch(sb, "-password", maskStoredPassword ? "******" : pass);
            }
        }

        // 服务器
        var serverAddress = ResolveServerAddress(account, global);
        if (!string.IsNullOrEmpty(serverAddress))
            AppendSwitch(sb, "-address", serverAddress);

        // Mod
        if (!string.IsNullOrEmpty(account.Mod))
            AppendSwitch(sb, "-mod", account.Mod);

        // 额外启动参数（去重 -w，过滤掉已废弃的 -legacy）
        if (!string.IsNullOrEmpty(account.Options))
        {
            var parts = account.Options.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                // 跳过 -w（已强制添加）和 -legacy（2026 已废弃）
                if (part.Equals("-w", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (part.Equals("-legacy", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (part.StartsWith("-", StringComparison.Ordinal))
                    flags.Add(part);

                sb.Append(' ').Append(part);
            }
        }

        // slave 账号强制低画质参数，并且保持在命令行末尾
        if (account.Role.Equals("slave", StringComparison.OrdinalIgnoreCase))
        {
            AppendFlagOnce(sb, flags, "-txt");
            AppendFlagOnce(sb, flags, "-lq");
            AppendFlagOnce(sb, flags, "-ns");
        }

        return sb.ToString();
    }

    private static string ResolveExePath(AccountConfig account, GlobalSettings global)
    {
        return !string.IsNullOrWhiteSpace(account.ExePathOverride)
            ? account.ExePathOverride
            : (!string.IsNullOrWhiteSpace(global.D2rExePath) ? global.D2rExePath : "D2R.exe");
    }

    private static string ResolveServerAddress(AccountConfig account, GlobalSettings global)
    {
        return !string.IsNullOrWhiteSpace(account.ServerAddress)
            ? account.ServerAddress.Trim()
            : global.BattleNetAddress.Trim();
    }

    private static void AppendSwitch(StringBuilder sb, string name, string value)
    {
        sb.Append(' ').Append(name).Append(' ');
        if (value.Any(char.IsWhiteSpace))
            sb.Append('"').Append(value).Append('"');
        else
            sb.Append(value);
    }

    private static void AppendFlagOnce(StringBuilder sb, HashSet<string> flags, string flag)
    {
        if (flags.Contains(flag))
            return;

        sb.Append(' ').Append(flag);
        flags.Add(flag);
    }
}
