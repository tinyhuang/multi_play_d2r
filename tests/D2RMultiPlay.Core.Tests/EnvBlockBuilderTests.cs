// ============================================================
// EnvBlockBuilderTests.cs — 环境块构造的单元测试
// ============================================================

using D2RMultiPlay.Core.Launch;

namespace D2RMultiPlay.Core.Tests;

public class EnvBlockBuilderTests
{
    [Fact]
    public void EnsureProfileDir_CreatesDirectoryStructure()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"d2r_test_{Guid.NewGuid()}");
        try
        {
            EnvBlockBuilder.EnsureProfileDir(tempRoot);

            var d2rDir = Path.Combine(tempRoot, "Saved Games", "Diablo II Resurrected");
            Assert.True(Directory.Exists(d2rDir));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void EnsureProfileDir_WithPreset_CopiesPresetFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"d2r_test_{Guid.NewGuid()}");
        var presetPath = Path.Combine(Path.GetTempPath(), $"d2r_preset_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(presetPath, """{"Window Mode": 0, "Resolution Width": 1280}""");

            EnvBlockBuilder.EnsureProfileDir(tempRoot, presetPath);

            var settingsPath = Path.Combine(tempRoot, "Saved Games", "Diablo II Resurrected", "Settings.json");
            Assert.True(File.Exists(settingsPath));

            var content = File.ReadAllText(settingsPath);
            Assert.Contains("1280", content);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
            if (File.Exists(presetPath)) File.Delete(presetPath);
        }
    }

    [Fact]
    public void EnsureProfileDir_CalledTwice_DoesNotOverwrite()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"d2r_test_{Guid.NewGuid()}");
        var presetPath = Path.Combine(Path.GetTempPath(), $"d2r_preset_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(presetPath, """{"original": true}""");
            EnvBlockBuilder.EnsureProfileDir(tempRoot, presetPath);

            // 修改预设内容
            File.WriteAllText(presetPath, """{"original": false}""");
            EnvBlockBuilder.EnsureProfileDir(tempRoot, presetPath);

            // 应该保留第一次的内容
            var settingsPath = Path.Combine(tempRoot, "Saved Games", "Diablo II Resurrected", "Settings.json");
            var content = File.ReadAllText(settingsPath);
            Assert.Contains("true", content);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
            if (File.Exists(presetPath)) File.Delete(presetPath);
        }
    }
}
