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
    public string AppAlreadyRunning => Get("AppAlreadyRunning");

    public string MenuFile => Get("MenuFile");
    public string MenuAccounts => Get("MenuAccounts");
    public string MenuTools => Get("MenuTools");
    public string MenuView => Get("MenuView");
    public string MenuGlobalSettings => Get("MenuGlobalSettings");
    public string MenuImport => Get("MenuImport");
    public string MenuExport => Get("MenuExport");
    public string MenuExit => Get("MenuExit");
    public string MenuLanguage => Get("MenuLanguage");
    public string MenuTheme => Get("MenuTheme");
    public string MenuThemeDark => Get("MenuThemeDark");
    public string MenuThemeLight => Get("MenuThemeLight");
    public string MenuIconStyle => Get("MenuIconStyle");
    public string MenuIconStyleGamer => Get("MenuIconStyleGamer");
    public string MenuIconStylePlain => Get("MenuIconStylePlain");
    public string MenuToggleLog => Get("MenuToggleLog");
    public string MenuQuickStart => Get("MenuQuickStart");
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
    public string BtnStop => Get("BtnStop");
    public string BtnDisabled => Get("BtnDisabled");

    public string ColId => Get("ColId");
    public string ColEnabled => Get("ColEnabled");
    public string ColName => Get("ColName");
    public string ColEmail => Get("ColEmail");
    public string ColRole => Get("ColRole");
    public string ColServer => Get("ColServer");
    public string ColMod => Get("ColMod");
    public string ColStatus => Get("ColStatus");
    public string ColActions => Get("ColActions");

    public string StatusAlive => Get("StatusAlive");
    public string StatusDead => Get("StatusDead");
    public string StatusDisabled => Get("StatusDisabled");

    public string HandleNotFound => Get("HandleNotFound");
    public string HandleNotFoundTitle => Get("HandleNotFoundTitle");
    public string HandleDownloadPrompt => Get("HandleDownloadPrompt");
    public string HandleRequiredReminder => Get("HandleRequiredReminder");
    public string HandleDownloadLink => Get("HandleDownloadLink");

    public string StatusReady => Get("StatusReady");
    public string StatusWaitingFormat => Get("StatusWaitingFormat");
    public string StatusConfigured => Get("StatusConfigured");
    public string StatusRequired => Get("StatusRequired");
    public string AlreadyRunningLog => Get("AlreadyRunningLog");
    public string ConfigImportedLog => Get("ConfigImportedLog");
    public string ConfigExportedLog => Get("ConfigExportedLog");
    public string MissingD2rPath => Get("MissingD2rPath");
    public string MissingHandlePath => Get("MissingHandlePath");
    public string LaunchPrereqMessage => Get("LaunchPrereqMessage");
    public string SelectHandleExeTitle => Get("SelectHandleExeTitle");
    public string SelectD2rExeTitle => Get("SelectD2rExeTitle");
    public string QuickD2rLabel => Get("QuickD2rLabel");
    public string QuickHandleLabel => Get("QuickHandleLabel");
    public string StartupNotice => Get("StartupNotice");
    public string QuickStartContent => Get("QuickStartContent");
    public string AboutContent => Get("AboutContent");
    public string IntlOnlyNote => Get("IntlOnlyNote");

    public string AccountEditorTitle => Get("AccountEditorTitle");
    public string LblUser => Get("LblUser");
    public string LblPassword => Get("LblPassword");
    public string LblName => Get("LblName");
    public string LblRole => Get("LblRole");
    public string LblMod => Get("LblMod");
    public string LblOptions => Get("LblOptions");
    public string LblExePath => Get("LblExePath");
    public string LblServerOverride => Get("LblServerOverride");
    public string ServerUseGlobalDefault => Get("ServerUseGlobalDefault");
    public string LblWindowWidth => Get("LblWindowWidth");
    public string LblWindowHeight => Get("LblWindowHeight");
    public string LblLaunchPreview => Get("LblLaunchPreview");
    public string PreviewLayoutHint => Get("PreviewLayoutHint");
    public string ChkEnabled => Get("ChkEnabled");
    public string RoleMaster => Get("RoleMaster");
    public string RoleSlave => Get("RoleSlave");

    public string GlobalSettingsTitle => Get("GlobalSettingsTitle");
    public string LblD2rExe => Get("LblD2rExe");
    public string LblHandleExe => Get("LblHandleExe");
    public string LblServer => Get("LblServer");
    public string ServerHint => Get("ServerHint");
    public string LblInterval => Get("LblInterval");
    public string LblMutexName => Get("LblMutexName");

    public string MonitorLayoutTitle => Get("MonitorLayoutTitle");
    public string BtnAutoGrid => Get("BtnAutoGrid");
    public string BtnRefreshMonitors => Get("BtnRefreshMonitors");

    public string ExportIncludePassword => Get("ExportIncludePassword");
    public string ExportPasswordWarning => Get("ExportPasswordWarning");

    // ===== Encrypted Import/Export =====
    public string MenuExportEncrypted => Get("MenuExportEncrypted");
    public string MenuImportEncrypted => Get("MenuImportEncrypted");
    public string ExportSetPassphraseTitle => Get("ExportSetPassphraseTitle");
    public string ExportPassphraseLabel => Get("ExportPassphraseLabel");
    public string ExportPassphraseConfirmLabel => Get("ExportPassphraseConfirmLabel");
    public string ExportIncludePasswordOption => Get("ExportIncludePasswordOption");
    public string ExportPassphraseWarning => Get("ExportPassphraseWarning");
    public string ExportPassphraseTooShort => Get("ExportPassphraseTooShort");
    public string ExportPassphraseMismatch => Get("ExportPassphraseMismatch");
    public string ImportEnterPassphraseTitle => Get("ImportEnterPassphraseTitle");
    public string ImportPassphraseLabel => Get("ImportPassphraseLabel");
    public string ImportWrongPassphrase => Get("ImportWrongPassphrase");
    public string ConfigExportedEncryptedLog => Get("ConfigExportedEncryptedLog");
    public string ConfigImportedEncryptedLog => Get("ConfigImportedEncryptedLog");

    public string LogLaunching => Get("LogLaunching");
    public string LogLaunched => Get("LogLaunched");
    public string LogArranging => Get("LogArranging");
    public string LogArranged => Get("LogArranged");
    public string LogFailed => Get("LogFailed");
    public string LogDied => Get("LogDied");
}
