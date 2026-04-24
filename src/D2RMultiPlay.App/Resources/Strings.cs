// ============================================================
// Strings.cs — i18n 字符串访问器
// 包装 ResourceManager 调用，统一从 .resx 读取当前语言资源
// ============================================================

namespace D2RMultiPlay.App.Resources;

internal sealed class Strings
{
    private static readonly System.Resources.ResourceManager _rm =
        new("D2RMultiPlay.App.Resources.Strings", typeof(Strings).Assembly);

    private static string Get(string key) => _rm.GetString(key) ?? key;

    public string AppTitle => Get("AppTitle");
    public string OK => Get("OK");
    public string Cancel => Get("Cancel");
    public string Save => Get("Save");
    public string Close => Get("Close");
    public string Error => Get("Error");
    public string Warning => Get("Warning");
    public string Info => Get("Info");

    public string MenuFile => Get("MenuFile");
    public string MenuGlobalSettings => Get("MenuGlobalSettings");
    public string MenuImport => Get("MenuImport");
    public string MenuExport => Get("MenuExport");
    public string MenuExit => Get("MenuExit");
    public string MenuLanguage => Get("MenuLanguage");
    public string MenuHelp => Get("MenuHelp");
    public string MenuAbout => Get("MenuAbout");
    public string MenuLayout => Get("MenuLayout");

    public string BtnLaunchAll => Get("BtnLaunchAll");
    public string BtnStopAll => Get("BtnStopAll");
    public string BtnAddAccount => Get("BtnAddAccount");
    public string BtnEdit => Get("BtnEdit");
    public string BtnDelete => Get("BtnDelete");
    public string BtnLaunch => Get("BtnLaunch");
    public string BtnReconnect => Get("BtnReconnect");

    public string ColId => Get("ColId");
    public string ColName => Get("ColName");
    public string ColRole => Get("ColRole");
    public string ColMod => Get("ColMod");
    public string ColStatus => Get("ColStatus");
    public string ColActions => Get("ColActions");

    public string StatusAlive => Get("StatusAlive");
    public string StatusDead => Get("StatusDead");
    public string StatusDisabled => Get("StatusDisabled");

    public string HandleNotFound => Get("HandleNotFound");
    public string HandleNotFoundTitle => Get("HandleNotFoundTitle");
    public string HandleDownloadPrompt => Get("HandleDownloadPrompt");

    public string AccountEditorTitle => Get("AccountEditorTitle");
    public string LblUser => Get("LblUser");
    public string LblPassword => Get("LblPassword");
    public string LblName => Get("LblName");
    public string LblRole => Get("LblRole");
    public string LblMod => Get("LblMod");
    public string LblOptions => Get("LblOptions");
    public string LblExePath => Get("LblExePath");
    public string RoleMaster => Get("RoleMaster");
    public string RoleSlave => Get("RoleSlave");

    public string GlobalSettingsTitle => Get("GlobalSettingsTitle");
    public string LblD2rExe => Get("LblD2rExe");
    public string LblHandleExe => Get("LblHandleExe");
    public string LblServer => Get("LblServer");
    public string LblInterval => Get("LblInterval");
    public string LblMutexName => Get("LblMutexName");

    public string MonitorLayoutTitle => Get("MonitorLayoutTitle");
    public string BtnAutoGrid => Get("BtnAutoGrid");
    public string BtnRefreshMonitors => Get("BtnRefreshMonitors");

    public string ExportIncludePassword => Get("ExportIncludePassword");
    public string ExportPasswordWarning => Get("ExportPasswordWarning");

    public string LogLaunching => Get("LogLaunching");
    public string LogLaunched => Get("LogLaunched");
    public string LogArranging => Get("LogArranging");
    public string LogArranged => Get("LogArranged");
    public string LogFailed => Get("LogFailed");
    public string LogDied => Get("LogDied");
}
