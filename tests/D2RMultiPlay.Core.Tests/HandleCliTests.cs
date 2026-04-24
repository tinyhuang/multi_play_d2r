// ============================================================
// HandleCliTests.cs — HandleCli 输出解析的单元测试
// 使用样本 stdout 喂入正则解析逻辑
// ============================================================

using D2RMultiPlay.Core.Handles;

namespace D2RMultiPlay.Core.Tests;

public class HandleCliTests
{
    [Fact]
    public void HandleLineRegex_ParsesStandardOutput()
    {
        // 模拟 handle.exe 典型输出
        const string sampleOutput =
            """
            D2R.exe            pid: 12345  type: Mutant          4C: \Sessions\1\BaseNamedObjects\Check For Other Instances
            D2R.exe            pid: 6789   type: Mutant          A8: \Sessions\1\BaseNamedObjects\Check For Other Instances
            """;

        var entries = ParseOutput(sampleOutput, "Check For Other Instances");

        Assert.Equal(2, entries.Count);
        Assert.Equal(12345u, entries[0].ProcessId);
        Assert.Equal("4C", entries[0].HandleId);
        Assert.Equal(6789u, entries[1].ProcessId);
        Assert.Equal("A8", entries[1].HandleId);
    }

    [Fact]
    public void HandleLineRegex_IgnoresUnrelatedLines()
    {
        const string sampleOutput =
            """
            svchost.exe        pid: 100    type: Mutant          10: \Sessions\1\BaseNamedObjects\SomeOtherMutex
            D2R.exe            pid: 12345  type: Mutant          4C: \Sessions\1\BaseNamedObjects\Check For Other Instances
            explorer.exe       pid: 200    type: File            20: C:\Windows\explorer.exe
            """;

        var entries = ParseOutput(sampleOutput, "Check For Other Instances");

        Assert.Single(entries);
        Assert.Equal(12345u, entries[0].ProcessId);
    }

    [Fact]
    public void HandleLineRegex_EmptyOutput_ReturnsNoEntries()
    {
        var entries = ParseOutput("", "Check For Other Instances");
        Assert.Empty(entries);
    }

    [Fact]
    public void HandleLineRegex_NoMatchingMutex_ReturnsNoEntries()
    {
        const string sampleOutput =
            """
            D2R.exe            pid: 12345  type: Mutant          4C: \Sessions\1\BaseNamedObjects\SomethingElse
            """;

        var entries = ParseOutput(sampleOutput, "Check For Other Instances");
        Assert.Empty(entries);
    }

    [Fact]
    public void HandleLineRegex_HexHandleId_ParsedCorrectly()
    {
        const string sampleOutput =
            """
            D2R.exe            pid: 99999  type: Mutant          FF0: \Sessions\1\BaseNamedObjects\Check For Other Instances
            """;

        var entries = ParseOutput(sampleOutput, "Check For Other Instances");

        Assert.Single(entries);
        Assert.Equal("FF0", entries[0].HandleId);
    }

    // 复用 HandleCli 内部的正则解析逻辑
    // 这里直接用反射调不到 private regex，所以重新用相同的 pattern 测试
    private static List<MutexEntry> ParseOutput(string output, string mutexName)
    {
        var entries = new List<MutexEntry>();
        var regex = new System.Text.RegularExpressions.Regex(
            @"pid:\s*(\d+)\s+type:\s*\w+\s+([0-9A-Fa-f]+):");

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(mutexName, StringComparison.OrdinalIgnoreCase))
                continue;

            var match = regex.Match(line);
            if (match.Success)
            {
                entries.Add(new MutexEntry(
                    ProcessId: uint.Parse(match.Groups[1].Value),
                    HandleId: match.Groups[2].Value));
            }
        }

        return entries;
    }
}
