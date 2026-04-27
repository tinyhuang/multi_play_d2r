// ============================================================
// Program.cs — 应用入口
// DPI 初始化 + 单实例互斥 + 语言设置
// ============================================================

using System.Globalization;
using System.Security.Principal;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.App;

internal static class Program
{
    /// <summary>单实例互斥量名称（避免多次打开管理器自身）</summary>
    private const string AppMutexName = "D2RMultiPlay_SingleInstance";
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "D2RMultiPlay",
        "startup.log");

    [STAThread]
    static void Main()
    {
        var args = Environment.GetCommandLineArgs();
        bool debugMode = args.Any(a => string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase));

        TraceStartup("========== App Start ==========");
        TraceStartup($"CommandLine: {string.Join(" ", args)}");
        TraceStartup($"OS: {Environment.OSVersion}");
        TraceStartup($"User: {Environment.UserName}, IsAdmin: {IsRunningAsAdministrator()}");

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, exArgs) =>
        {
            TraceStartup("Application.ThreadException", exArgs.Exception);
            MessageBox.Show(
                "Unhandled UI exception.\n\n" + exArgs.Exception.Message + $"\n\nLog: {StartupLogPath}",
                "Runtime Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, exArgs) =>
        {
            TraceStartup("AppDomain.UnhandledException", exArgs.ExceptionObject as Exception);
        };

        try
        {
            if (debugMode)
            {
                MessageBox.Show(
                    "Debug mode enabled.\n" +
                    "Startup logs will be written to:\n" + StartupLogPath,
                    "D2R Multi-Play Debug",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            // 单实例检查
            using var mutex = new Mutex(true, AppMutexName, out bool createdNew);
            TraceStartup($"Single instance check result: createdNew={createdNew}");
            if (!createdNew)
            {
                MessageBox.Show(
                    "D2R Multi-Play Manager is already running.",
                    "D2R Multi-Play Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TraceStartup("Exit because another instance is already running.");
                return;
            }

            // 加载配置以获取语言偏好
            var config = ConfigStore.Load();
            TraceStartup($"Config loaded. UiCulture={config.Global.UiCulture}, UiTheme={config.Global.UiTheme}, IconStyle={config.Global.IconStyle}");
            ApplyCulture(config.Global.UiCulture);
            TraceStartup($"Culture applied: {Thread.CurrentThread.CurrentUICulture.Name}");

            ApplicationConfiguration.Initialize();
            TraceStartup("ApplicationConfiguration initialized.");

            using var mainForm = new MainForm(config);
            mainForm.Load += (_, _) => TraceStartup("MainForm.Load fired.");
            mainForm.Shown += (_, _) => TraceStartup("MainForm.Shown fired.");

            TraceStartup("Entering Application.Run(mainForm).");
            Application.Run(mainForm);
            TraceStartup("Application.Run exited normally.");
        }
        catch (Exception ex)
        {
            TraceStartup("Startup exception", ex);
            MessageBox.Show(
                "Failed to start D2R Multi-Play Manager.\n\n" + ex.Message + $"\n\nLog: {StartupLogPath}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void TraceStartup(string message, Exception? ex = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(StartupLogPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var sw = new StreamWriter(StartupLogPath, append: true);
            sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            if (ex != null)
                sw.WriteLine(ex.ToString());
        }
        catch
        {
            // Swallow logging errors to avoid secondary startup failures.
        }
    }

    /// <summary>
    /// 设置 UI 线程的 Culture，驱动 .resx 资源切换
    /// </summary>
    internal static void ApplyCulture(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // 回退到 zh-CN
            var fallback = new CultureInfo("zh-CN");
            Thread.CurrentThread.CurrentUICulture = fallback;
        }
    }
}
