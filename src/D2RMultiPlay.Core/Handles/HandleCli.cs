// ============================================================
// HandleCli.cs — 封装 Sysinternals handle.exe 的调用与输出解析
// 用于合规地关闭 D2R 的 "Check For Other Instances" 互斥量
// ============================================================

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace D2RMultiPlay.Core.Handles;

/// <summary>
/// handle.exe 查找到的一条互斥量匹配记录
/// </summary>
public sealed record MutexEntry(uint ProcessId, string HandleId);

/// <summary>
/// handle.exe 操作结果
/// </summary>
public sealed class HandleResult
{
    public bool Success { get; init; }
    public string Output { get; init; } = "";
    public string Error { get; init; } = "";
    public int ExitCode { get; init; }
}

public static partial class HandleCli
{
    /// <summary>handle.exe 官方下载页面</summary>
    public const string DownloadUrl = "https://learn.microsoft.com/en-us/sysinternals/downloads/handle";

    // 解析 handle.exe 输出的正则（示例行）:
    // D2R.exe           pid: 12345  type: Mutant    4C: \Sessions\1\BaseNamedObjects\Check For Other Instances
    // 我们需要提取: pid=12345, handleId=4C
    [GeneratedRegex(@"pid:\s*(\d+)\s+type:\s*\w+\s+([0-9A-Fa-f]+):", RegexOptions.Compiled)]
    private static partial Regex HandleLineRegex();

    /// <summary>
    /// 检查 handle.exe 是否存在于指定路径
    /// </summary>
    public static bool Exists(string handleExePath)
    {
        return !string.IsNullOrWhiteSpace(handleExePath) && File.Exists(handleExePath);
    }

    /// <summary>
    /// 查找所有持有指定互斥量的进程句柄
    /// </summary>
    /// <param name="handleExePath">handle.exe 完整路径</param>
    /// <param name="mutexName">互斥量名称（默认 "Check For Other Instances"）</param>
    /// <returns>匹配的互斥量条目列表 + 原始输出</returns>
    public static (List<MutexEntry> Entries, HandleResult Result) FindMutex(
        string handleExePath, string mutexName = "Check For Other Instances")
    {
        // 构建命令行参数：-accepteula 首次自动接受许可证, -a 搜索所有句柄, -nobanner 抑制版本横幅
        var args = $"-accepteula -a \"{mutexName}\" -nobanner";
        var result = RunHandle(handleExePath, args);
        var entries = new List<MutexEntry>();

        if (!result.Success && result.ExitCode != 0)
            return (entries, result);

        // 逐行解析输出
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // 确保这一行确实包含我们要找的互斥量名
            if (!line.Contains(mutexName, StringComparison.OrdinalIgnoreCase))
                continue;

            var match = HandleLineRegex().Match(line);
            if (match.Success)
            {
                entries.Add(new MutexEntry(
                    ProcessId: uint.Parse(match.Groups[1].Value),
                    HandleId: match.Groups[2].Value));
            }
        }

        return (entries, result);
    }

    /// <summary>
    /// 关闭指定进程中的一个句柄
    /// </summary>
    public static HandleResult CloseHandle(string handleExePath, uint processId, string handleId)
    {
        var args = $"-accepteula -p {processId} -c {handleId} -y -nobanner";
        return RunHandle(handleExePath, args);
    }

    /// <summary>
    /// 一键流程：查找并关闭所有匹配的互斥量句柄
    /// </summary>
    /// <returns>关闭的句柄数量 + 详细日志</returns>
    public static (int ClosedCount, List<string> Log) FindAndCloseAll(
        string handleExePath, string mutexName = "Check For Other Instances")
    {
        var log = new List<string>();
        var (entries, findResult) = FindMutex(handleExePath, mutexName);

        if (!findResult.Success)
        {
            log.Add($"[错误/Error] handle.exe 执行失败 (exit={findResult.ExitCode}): {findResult.Error}");
            return (0, log);
        }

        if (entries.Count == 0)
        {
            log.Add("[信息/Info] 未发现活跃的互斥量句柄 / No active mutex handles found.");
            return (0, log);
        }

        log.Add($"[信息/Info] 发现 {entries.Count} 个互斥量句柄 / Found {entries.Count} mutex handle(s).");

        int closed = 0;
        foreach (var entry in entries)
        {
            var closeResult = CloseHandle(handleExePath, entry.ProcessId, entry.HandleId);
            if (closeResult.Success || closeResult.ExitCode == 0)
            {
                log.Add($"  ✓ 已关闭 PID={entry.ProcessId} Handle={entry.HandleId}");
                closed++;
            }
            else
            {
                log.Add($"  ✗ 关闭失败 PID={entry.ProcessId} Handle={entry.HandleId}: {closeResult.Error}");
            }
        }

        return (closed, log);
    }

    // ---- 内部：执行 handle.exe 并捕获输出 ----

    private static HandleResult RunHandle(string exePath, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,          // 不弹黑窗
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(15_000); // 最多等 15 秒

            return new HandleResult
            {
                Success = process.ExitCode == 0,
                Output = stdout,
                Error = stderr,
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new HandleResult
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }
}
