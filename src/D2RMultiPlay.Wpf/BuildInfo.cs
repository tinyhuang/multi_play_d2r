using System.Reflection;

namespace D2RMultiPlay.Wpf;

internal sealed class BuildInfo
{
    public string Version { get; init; } = "0.0.0";
    public string InformationalVersion { get; init; } = "0.0.0-local";
    public string Display { get; init; } = "0.0.0-local";

    public static BuildInfo ReadCurrent()
    {
        var asm = Assembly.GetExecutingAssembly();

        var infoVersion = asm
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        var fileVersion = asm
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;

        var displayVersion = string.IsNullOrWhiteSpace(fileVersion)
            ? "0.0.0"
            : fileVersion;

        return new BuildInfo
        {
            Version = displayVersion,
            InformationalVersion = string.IsNullOrWhiteSpace(infoVersion) ? displayVersion : infoVersion,
            Display = string.IsNullOrWhiteSpace(infoVersion)
                ? displayVersion
                : $"{displayVersion} ({infoVersion})"
        };
    }
}
