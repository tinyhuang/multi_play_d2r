// ============================================================
// App.xaml.cs — WPF 应用启动入口
// 单实例互斥 + 配置加载 + 语言设置
// ============================================================

using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>单实例互斥量名称</summary>
    private const string AppMutexName = "D2RMultiPlay_SingleInstance";
    
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "D2RMultiPlay",
        "startup.log");

    /// <summary>全局配置实例（在 OnStartup 中初始化）</summary>
    public static AppConfig? GlobalConfig { get; private set; }

    private Mutex? _appMutex;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, exArgs) =>
        {
            TraceStartup("Unhandled exception", exArgs.ExceptionObject as Exception);
        };

        DispatcherUnhandledException += (_, exArgs) =>
        {
            TraceStartup("Dispatcher exception", exArgs.Exception);
            exArgs.Handled = true;
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool debugMode = e.Args.Any(a => string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase));

        TraceStartup("========== App Start (WPF) ==========");
        TraceStartup($"CommandLine: {string.Join(" ", e.Args)}");
        TraceStartup($"OS: {Environment.OSVersion}");
        TraceStartup($"User: {Environment.UserName}, IsAdmin: {IsRunningAsAdministrator()}");

        try
        {
            if (debugMode)
            {
                MessageBox.Show(
                    "Debug mode enabled.\nStartup logs will be written to:\n" + StartupLogPath,
                    "D2R Multi-Play Debug",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // 单实例检查
            _appMutex = new Mutex(true, AppMutexName, out bool createdNew);
            TraceStartup($"Single instance check result: createdNew={createdNew}");
            if (!createdNew)
            {
                MessageBox.Show(
                    "D2R Multi-Play Manager is already running.",
                    "D2R Multi-Play Manager",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                TraceStartup("Exit because another instance is already running.");
                Shutdown();
                return;
            }

            // 加载配置以获取语言偏好
            GlobalConfig = ConfigStore.Load();
            TraceStartup($"Config loaded. UiCulture={GlobalConfig.Global.UiCulture}, UiTheme={GlobalConfig.Global.UiTheme}, IconStyle={GlobalConfig.Global.IconStyle}");
            ApplyCulture(GlobalConfig.Global.UiCulture);
            TraceStartup($"Culture applied: {Thread.CurrentThread.CurrentUICulture.Name}");

            TraceStartup("App.OnStartup completed successfully.");
        }
        catch (Exception ex)
        {
            TraceStartup("Startup exception", ex);
            MessageBox.Show(
                "Failed to start D2R Multi-Play Manager.\n\n" + ex.Message + $"\n\nLog: {StartupLogPath}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TraceStartup("App.OnExit fired.");
        _appMutex?.Dispose();
        base.OnExit(e);
    }

    public static void ApplyCulture(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch
        {
            // 无法识别的语言代码，使用默认
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static void TraceStartup(string message, Exception? ex = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(StartupLogPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            var line = ex == null
                ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}"
                : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n{ex}";

            File.AppendAllText(StartupLogPath, line + Environment.NewLine);
        }
        catch
        {
            // Ignore logging failures
        }
    }
}
