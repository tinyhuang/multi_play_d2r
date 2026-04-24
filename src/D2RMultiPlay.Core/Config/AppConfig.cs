// ============================================================
// AppConfig.cs — 根配置 POCO + JSON 源生成器
// 所有持久化配置的数据结构定义
// ============================================================

using System.Text.Json.Serialization;

namespace D2RMultiPlay.Core.Config;

/// <summary>
/// 应用根配置（对应 config.json 顶层结构）
/// </summary>
public sealed class AppConfig
{
    /// <summary>配置文件格式版本号，用于未来迁移</summary>
    public int Version { get; set; } = 1;

    /// <summary>全局设置</summary>
    public GlobalSettings Global { get; set; } = new();

    /// <summary>检测到的显示器信息（启动时刷新）</summary>
    public List<MonitorConfig> Monitors { get; set; } = [];

    /// <summary>账号列表</summary>
    public List<AccountConfig> Accounts { get; set; } = [];
}

/// <summary>
/// 全局设置
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>D2R 主程序路径（全局默认）</summary>
    public string D2rExePath { get; set; } = "";

    /// <summary>handle.exe 路径（Sysinternals）</summary>
    public string HandleExePath { get; set; } = "";

    /// <summary>战网服务器地址</summary>
    public string BattleNetAddress { get; set; } = "kr.actual.battle.net";

    /// <summary>启动实例间隔（秒）</summary>
    public int LaunchIntervalSec { get; set; } = 8;

    /// <summary>账号 profile 存储根目录（默认 %APPDATA%\D2RMultiPlay\profiles）</summary>
    public string ProfilesRoot { get; set; } = "";

    /// <summary>D2R 互斥量名称（暴雪若改名可在此自定义）</summary>
    public string MutexName { get; set; } = "Check For Other Instances";

    /// <summary>挂机窗口 CPU 亲和性掩码（0=不限制）</summary>
    public ulong SlaveAffinityMask { get; set; }

    /// <summary>UI 语言代码（zh-CN / en-US）</summary>
    public string UiCulture { get; set; } = "zh-CN";
}

/// <summary>
/// 显示器配置快照
/// </summary>
public sealed class MonitorConfig
{
    /// <summary>设备名（如 \\.\DISPLAY1），用作稳定标识</summary>
    public string Id { get; set; } = "";

    /// <summary>显示器完整边界 [x, y, w, h]</summary>
    public int[] Bounds { get; set; } = [0, 0, 1920, 1080];

    /// <summary>工作区（去任务栏）[x, y, w, h]</summary>
    public int[] WorkArea { get; set; } = [0, 0, 1920, 1040];

    /// <summary>DPI 缩放比（1.0 = 100%）</summary>
    public double DpiScale { get; set; } = 1.0;

    /// <summary>是否主显示器</summary>
    public bool IsPrimary { get; set; }

    /// <summary>刷新率 (Hz)</summary>
    public uint RefreshHz { get; set; } = 60;
}

/// <summary>
/// 单个账号配置
/// </summary>
public sealed class AccountConfig
{
    /// <summary>账号序号（1-8）</summary>
    public int Id { get; set; }

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; }

    /// <summary>显示名称（仅 UI 展示用）</summary>
    public string Name { get; set; } = "";

    /// <summary>角色：master（主玩）或 slave（挂机）</summary>
    public string Role { get; set; } = "master";

    /// <summary>战网登录用户名（邮箱）</summary>
    public string User { get; set; } = "";

    /// <summary>DPAPI 加密后的密码（Base64）</summary>
    public string PassEnc { get; set; } = "";

    /// <summary>Mod 名称（留空不加载 Mod）</summary>
    public string Mod { get; set; } = "";

    /// <summary>额外启动参数（如 -txt -ns -lq）</summary>
    public string Options { get; set; } = "";

    /// <summary>独立游戏路径（留空使用全局 D2rExePath）</summary>
    public string ExePathOverride { get; set; } = "";

    /// <summary>窗口布局配置</summary>
    public WindowLayout Layout { get; set; } = new();
}

/// <summary>
/// 窗口布局（绑定到某个显示器的特定位置）
/// </summary>
public sealed class WindowLayout
{
    /// <summary>目标显示器设备名</summary>
    public string MonitorId { get; set; } = "";

    /// <summary>窗口左上角 X 坐标（屏幕绝对像素）</summary>
    public int X { get; set; }

    /// <summary>窗口左上角 Y 坐标</summary>
    public int Y { get; set; }

    /// <summary>窗口宽度</summary>
    public int W { get; set; } = 1280;

    /// <summary>窗口高度</summary>
    public int H { get; set; } = 720;

    /// <summary>是否去边框</summary>
    public bool Borderless { get; set; } = true;
}

/// <summary>
/// System.Text.Json 源生成器上下文 — AOT 兼容，零反射序列化
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(GlobalSettings))]
[JsonSerializable(typeof(MonitorConfig))]
[JsonSerializable(typeof(AccountConfig))]
[JsonSerializable(typeof(WindowLayout))]
public partial class AppConfigJsonContext : JsonSerializerContext;
