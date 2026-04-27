// ============================================================
// Program.cs — 应用入口
// DPI 初始化 + 单实例互斥 + 语言设置
// ============================================================

using System.Globalization;
using D2RMultiPlay.Core.Config;

namespace D2RMultiPlay.App;

internal static class Program
{
    /// <summary>单实例互斥量名称（避免多次打开管理器自身）</summary>
    private const string AppMutexName = "D2RMultiPlay_SingleInstance";

    [STAThread]
    static void Main()
    {
        try
        {
            // 单实例检查
            using var mutex = new Mutex(true, AppMutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "D2R Multi-Play Manager is already running.",
                    "D2R Multi-Play Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 加载配置以获取语言偏好
            var config = ConfigStore.Load();
            ApplyCulture(config.Global.UiCulture);

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(config));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Failed to start D2R Multi-Play Manager.\n\n" + ex.Message,
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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
