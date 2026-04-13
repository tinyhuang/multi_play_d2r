@echo off
:: ==========================================
:: Debug Mode Settings
:: Set to 1 to see all commands printed (useful for tracing errors)
:: ==========================================
set DEBUG_MODE=0
if "%DEBUG_MODE%"=="1" (
    @echo on
)

setlocal enabledelayedexpansion
chcp 65001 >nul
pushd "%~dp0"

:: ==========================================
:: Load Configuration
:: ==========================================
if not exist "config\base_settings.bat" (
    echo [Error] "config\base_settings.bat" not found! 
    echo Please copy "config\base_settings.bat.example" to "config\base_settings.bat" and configure it.
    pause
    exit /b
)

if not exist "config\accounts_secrets.bat" (
    echo [Error] "config\accounts_secrets.bat" not found! 
    echo Please copy "config\accounts_secrets.bat.example" to "config\accounts_secrets.bat" and set your credentials.
    pause
    exit /b
)

:: Call configurations
:: Normalize line endings first (LF -> CRLF) so cmd.exe can parse them correctly.
:: This handles config files created or edited on macOS/Linux where line endings are LF-only.
:: The 'more' command is a Windows text filter that always outputs CRLF.
more < "config\base_settings.bat" > "%TEMP%\_d2r_base.bat" 2>nul
more < "config\accounts_secrets.bat" > "%TEMP%\_d2r_acct.bat" 2>nul
call "%TEMP%\_d2r_base.bat"
call "%TEMP%\_d2r_acct.bat"
del /q "%TEMP%\_d2r_base.bat" "%TEMP%\_d2r_acct.bat" 2>nul

:: Change to Working Directory
D:
cd "%workdir%"

echo =========================================================
echo [Info] Starting D2R Multi-Play Launcher...
echo =========================================================

set LAUNCHED_COUNT=0

:: ==========================================
:: Multi-Monitor Setup: backward compatibility + N-monitor support
:: MONITOR_IDS should match Windows Display numbers (Settings > System > Display)
:: ==========================================
if not defined MONITOR_IDS (
    if defined MONITOR_COUNT (
        :: Convert MONITOR_COUNT to MONITOR_IDS (e.g. 3 -> "1 2 3")
        set "MONITOR_IDS="
        for /L %%M in (1,1,!MONITOR_COUNT!) do set "MONITOR_IDS=!MONITOR_IDS! %%M"
        echo [Info] Converted MONITOR_COUNT=!MONITOR_COUNT! to MONITOR_IDS=!MONITOR_IDS!
    ) else if defined SCREEN_W (
        :: Legacy mode: build monitor config from old SCREEN_W/H variables
        set "MONITOR_IDS=1"
        if not defined DISPLAY1_SCALE set DISPLAY1_SCALE=100
        set "MON_1_W=!SCREEN_W!"
        set "MON_1_H=!SCREEN_H!"
        set "MON_1_SCALE=!DISPLAY1_SCALE!"
        set "MON_1_X=0"
        set "MON_1_Y=0"
        if not defined TASKBAR_H set TASKBAR_H=0
        set "MON_1_TASKBAR=!TASKBAR_H!"
        if defined DISPLAY2_SCALE (
            set "MONITOR_IDS=1 2"
            set "MON_2_W=!SCREEN_W!"
            set "MON_2_H=!SCREEN_H!"
            set "MON_2_SCALE=!DISPLAY2_SCALE!"
            if not defined MONITOR2_Y_OFFSET set MONITOR2_Y_OFFSET=0
            set "MON_2_X=0"
            set "MON_2_Y=!MONITOR2_Y_OFFSET!"
            set "MON_2_TASKBAR=0"
        )
        echo [Info] Legacy monitor config detected. MONITOR_IDS=!MONITOR_IDS!
    ) else (
        set "MONITOR_IDS=1"
        echo [Info] No monitor config found. Defaulting to MONITOR_IDS=1
    )
)

:: Determine first monitor ID (used as default fallback)
for %%M in (!MONITOR_IDS!) do (
    if not defined FIRST_MON_ID set "FIRST_MON_ID=%%M"
)

:: Default window size
if not defined DEFAULT_WIN_W set DEFAULT_WIN_W=1280
if not defined DEFAULT_WIN_H set DEFAULT_WIN_H=720
if not defined MIN_WIN_W set MIN_WIN_W=800
if not defined MIN_WIN_H set MIN_WIN_H=600
if not defined PRIMARY_WIN_W set PRIMARY_WIN_W=1920
if not defined PRIMARY_WIN_H set PRIMARY_WIN_H=1080

:: ==========================================
:: Phase 1: Auto-assign MONITOR for accounts that left it blank
::          Strategy: assign to the monitor with fewest windows (round-robin on ties)
:: ==========================================
:: First pass: init counters and count explicitly assigned windows per monitor
for %%M in (!MONITOR_IDS!) do set "MON_%%M_TOTAL=0"

:: Build a lookup string to validate monitor IDs (e.g. " 1 3 4 ")
set "VALID_MONS= "
for %%M in (!MONITOR_IDS!) do set "VALID_MONS=!VALID_MONS!%%M "

for %%I in (1 2 3 4 5 6 7 8) do (
    set "CHK_ENABLE=!ACCOUNT_%%I_ENABLE!"
    set "CHK_ENABLE=!CHK_ENABLE:~0,1!"
    if "!CHK_ENABLE!"=="1" (
        set "CHK_MON=!ACCOUNT_%%I_MONITOR!"
        if defined CHK_MON (
            :: Validate: is this monitor ID in MONITOR_IDS?
            set "_found=0"
            for %%M in (!MONITOR_IDS!) do (
                if "!CHK_MON!"=="%%M" set "_found=1"
            )
            if "!_found!"=="0" (
                echo [Warn] Account %%I assigned to Display !CHK_MON! which is not in MONITOR_IDS=!MONITOR_IDS!. Reassigning to Display !FIRST_MON_ID!.
                set "ACCOUNT_%%I_MONITOR=!FIRST_MON_ID!"
                set /a "MON_!FIRST_MON_ID!_TOTAL+=1"
            ) else (
                set /a "MON_!CHK_MON!_TOTAL+=1"
            )
        )
    )
)

:: Second pass: assign unassigned accounts to least-loaded monitor
for %%I in (1 2 3 4 5 6 7 8) do (
    set "CHK_ENABLE=!ACCOUNT_%%I_ENABLE!"
    set "CHK_ENABLE=!CHK_ENABLE:~0,1!"
    if "!CHK_ENABLE!"=="1" (
        set "CHK_MON=!ACCOUNT_%%I_MONITOR!"
        if not defined CHK_MON (
            :: Find monitor with fewest windows
            set "BEST_MON=!FIRST_MON_ID!"
            set "BEST_COUNT=!MON_%FIRST_MON_ID%_TOTAL!"
            for %%M in (!MONITOR_IDS!) do (
                if !MON_%%M_TOTAL! lss !BEST_COUNT! (
                    set "BEST_MON=%%M"
                    set "BEST_COUNT=!MON_%%M_TOTAL!"
                )
            )
            set "ACCOUNT_%%I_MONITOR=!BEST_MON!"
            set /a "MON_!BEST_MON!_TOTAL+=1"
        )
    )
)

:: ==========================================
:: Phase 2: Calculate grid layout for each monitor
:: ==========================================
for %%M in (!MONITOR_IDS!) do (
    call :CalcMonitorGrid %%M
)

:: Initialize per-monitor placement counters
for %%M in (!MONITOR_IDS!) do set "MON_%%M_PLACED=0"

:: Perform 1-time backup of Global Settings.json to preserve main account layout
set "GLOBAL_D2R_SAVE_PATH=%USERPROFILE%\Saved Games\Diablo II Resurrected"
if exist "!GLOBAL_D2R_SAVE_PATH!\Settings.json" (
    if not exist "!GLOBAL_D2R_SAVE_PATH!\Settings_Original_Backup.json" (
        echo [Info] Backing up original Settings.json to prevent unwanted overwrite...
        copy /Y "!GLOBAL_D2R_SAVE_PATH!\Settings.json" "!GLOBAL_D2R_SAVE_PATH!\Settings_Original_Backup.json" >nul
    )
)

for %%I in (1 2 3 4 5 6 7 8) do call :CheckAndLaunch %%I

echo =========================================================
echo [Success] All requested D2R instances have been launched and arranged!
echo [Info] Total instances launched: %LAUNCHED_COUNT%
echo =========================================================

:: Wait for user input to prevent the window from closing immediately, useful for debugging
echo.
echo [Info] Script execution finished. Press any key to exit...
pause
endlocal
goto :EOF

:: ==========================================
:: Subroutine: CheckAndLaunch
:: Usage: call :CheckAndLaunch <account_number>
:: Launches game instance and positions window on assigned monitor.
:: ==========================================
:CheckAndLaunch
set "ACCT_ID=%~1"

:: Dynamically read account variables using delayed expansion
set "ACCT_ENABLE=!ACCOUNT_%ACCT_ID%_ENABLE!"
set "ACCT_USER=!ACCOUNT_%ACCT_ID%_USER!"
set "ACCT_PASS=!ACCOUNT_%ACCT_ID%_PASS!"
set "ACCT_MOD=!ACCOUNT_%ACCT_ID%_MOD!"
set "ACCT_OPTIONS=!ACCOUNT_%ACCT_ID%_OPTIONS!"
set "ACCT_MONITOR=!ACCOUNT_%ACCT_ID%_MONITOR!"
set "ACCT_DIABLO=!ACCOUNT_%ACCT_ID%_DIABLO!"
set "ACCT_PRIMARY=!ACCOUNT_%ACCT_ID%_PRIMARY!"

:: Robust check: only take the first character to avoid trailing whitespace/CR issues
set "ENABLE_FLAG=!ACCT_ENABLE:~0,1!"
if not "!ENABLE_FLAG!"=="1" (
    echo [Info] Account %ACCT_ID% is disabled. Skipping...
    exit /b
)

:: ====== Resolve monitor assignment and calculate window position ======
set "M=!ACCT_MONITOR!"
if not defined M set "M=1"
set "M=!M:~0,1!"

:: Determine window size based on PRIMARY flag
set "PRI_FLAG=!ACCT_PRIMARY:~0,1!"
if "!PRI_FLAG!"=="1" (
    set "pos_w=!PRIMARY_WIN_W!"
    set "pos_h=!PRIMARY_WIN_H!"
    if not defined pos_w set "pos_w=1920"
    if not defined pos_h set "pos_h=1080"
) else (
    set "pos_w=!MON_%M%_WIN_W!"
    set "pos_h=!MON_%M%_WIN_H!"
)
set "GRID_C=!MON_%M%_GRID_COLS!"
set "GRID_R=!MON_%M%_GRID_ROWS!"
set "STEP_X=!MON_%M%_STEP_X!"
set "STEP_Y=!MON_%M%_STEP_Y!"
set "BASE_X=!MON_%M%_X!"
set "BASE_Y=!MON_%M%_Y!"
set "SAFE_X=!MON_%M%_SAFE_X!"
set "SAFE_Y=!MON_%M%_SAFE_Y!"
set "PLACED=!MON_%M%_PLACED!"

:: Calculate grid position for this window
set /a "col=PLACED %% GRID_C"
set /a "row=PLACED / GRID_C"
set /a "pos_x=BASE_X + col * STEP_X"
set /a "pos_y=BASE_Y + row * STEP_Y"

:: Bounds check: keep window within monitor area
set /a "max_x=BASE_X + SAFE_X"
set /a "max_y=BASE_Y + SAFE_Y"
if !pos_x! gtr !max_x! set "pos_x=!max_x!"
if !pos_y! gtr !max_y! set "pos_y=!max_y!"

set /a "MON_%M%_PLACED+=1"
set "_role=non-play"
if "!PRI_FLAG!"=="1" set "_role=PLAY"
echo [Info] Account !ACCT_ID! ^(!ACCT_MOD!^) [!_role!] -^> Monitor !M! pos: !pos_x!,!pos_y! size: !pos_w!x!pos_h!

:: Define a separate profile directory for this account to isolate Settings
set "FAKE_PROFILE=%workdir%\profiles\account_!ACCT_ID!"
set "D2R_SAVE_PATH=!FAKE_PROFILE!\Saved Games\Diablo II Resurrected"

:: Create the faux profile directory if it does not exist
if not exist "!D2R_SAVE_PATH!" (
    mkdir "!D2R_SAVE_PATH!"
    
    :: Attempt to copy the system's default Settings.json as a base configuration
    if exist "!GLOBAL_D2R_SAVE_PATH!\Settings_Original_Backup.json" (
        echo [Info] Copying default Settings_Original_Backup.json for Account !ACCT_ID!...
        copy "!GLOBAL_D2R_SAVE_PATH!\Settings_Original_Backup.json" "!D2R_SAVE_PATH!\Settings.json" >nul
    ) else if exist "!GLOBAL_D2R_SAVE_PATH!\Settings.json" (
        echo [Info] Copying default Settings.json for Account !ACCT_ID!...
        copy "!GLOBAL_D2R_SAVE_PATH!\Settings.json" "!D2R_SAVE_PATH!\Settings.json" >nul
    )
)

echo [Info] Checking handles for Instance !ACCT_ID!...
%myhandler% -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y

:: ==== [CONFIG ISOLATION SWAP] ====
:: D2R strictly reads from the Global registry path on load.
:: Before launching, we inject this account's specific Settings.json into the Global path.
if exist "!D2R_SAVE_PATH!\Settings.json" (
    echo [Info] Activating dedicated Settings.json for Account !ACCT_ID! into global slot...
    if not exist "!GLOBAL_D2R_SAVE_PATH!" mkdir "!GLOBAL_D2R_SAVE_PATH!"
    copy /Y "!D2R_SAVE_PATH!\Settings.json" "!GLOBAL_D2R_SAVE_PATH!\Settings.json" >nul
)

:: Resolve per-account or global game executable path
set "ACCT_DIABLO_RESOLVED=!ACCT_DIABLO!"
if not defined ACCT_DIABLO_RESOLVED set "ACCT_DIABLO_RESOLVED=%diablo%"
if not defined ACCT_DIABLO_RESOLVED (
    echo [Error] Account !ACCT_ID!: No game executable path configured. Skipping...
    echo         Please set ACCOUNT_!ACCT_ID!_DIABLO or the global 'diablo' variable.
    exit /b
)
if not exist !ACCT_DIABLO_RESOLVED! (
    echo [Error] Account !ACCT_ID!: Game executable not found: !ACCT_DIABLO_RESOLVED!
    echo         Please verify ACCOUNT_!ACCT_ID!_DIABLO or the global 'diablo' path.
    exit /b
)

:: Start the game using a wrapper command to inject the custom USERPROFILE environment variable
cmd /c "set USERPROFILE=!FAKE_PROFILE! && start "" !ACCT_DIABLO_RESOLVED! -username !ACCT_USER! -password !ACCT_PASS! -address %addres% -mod !ACCT_MOD! -w !ACCT_OPTIONS!"
echo [Info] Instance !ACCT_ID! launched (exe: !ACCT_DIABLO_RESOLVED!). Waiting %secs% seconds before adjusting window...
timeout /T %secs%

if "%ACCT_ID%"=="1" set TITLE_NAME=one
if "%ACCT_ID%"=="2" set TITLE_NAME=two
if "%ACCT_ID%"=="3" set TITLE_NAME=three
if "%ACCT_ID%"=="4" set TITLE_NAME=four
if "%ACCT_ID%"=="5" set TITLE_NAME=five
if "%ACCT_ID%"=="6" set TITLE_NAME=six
if "%ACCT_ID%"=="7" set TITLE_NAME=seven
if "%ACCT_ID%"=="8" set TITLE_NAME=eight
newtitle "%TITLE_NAME%" !pos_x! !pos_y! !pos_w! !pos_h!

set /a LAUNCHED_COUNT+=1
exit /b

:: ==========================================
:: Subroutine: CalcMonitorGrid
:: Usage: call :CalcMonitorGrid <monitor_number>
:: Calculates grid layout, window size, and step values for a monitor.
:: Reads: MON_M_W, MON_M_H, MON_M_SCALE, MON_M_X, MON_M_Y, MON_M_TASKBAR, MON_M_TOTAL
:: Writes: MON_M_GRID_COLS, MON_M_GRID_ROWS, MON_M_WIN_W, MON_M_WIN_H,
::         MON_M_STEP_X, MON_M_STEP_Y, MON_M_SAFE_X, MON_M_SAFE_Y
:: ==========================================
:CalcMonitorGrid
set "MG=%~1"
set "MG_TOTAL=!MON_%MG%_TOTAL!"
if not defined MG_TOTAL set MG_TOTAL=0
if !MG_TOTAL! equ 0 (
    :: No windows on this monitor, set safe defaults
    set "MON_%MG%_GRID_COLS=1"
    set "MON_%MG%_GRID_ROWS=1"
    set "MON_%MG%_WIN_W=!DEFAULT_WIN_W!"
    set "MON_%MG%_WIN_H=!DEFAULT_WIN_H!"
    set "MON_%MG%_STEP_X=0"
    set "MON_%MG%_STEP_Y=0"
    set "MON_%MG%_SAFE_X=0"
    set "MON_%MG%_SAFE_Y=0"
    exit /b
)

:: Read monitor properties
set "MG_W=!MON_%MG%_W!"
set "MG_H=!MON_%MG%_H!"
set "MG_SCALE=!MON_%MG%_SCALE!"
set "MG_TB=!MON_%MG%_TASKBAR!"
if not defined MG_W set MG_W=1920
if not defined MG_H set MG_H=1080
if not defined MG_SCALE set MG_SCALE=100
if not defined MG_TB set MG_TB=0

:: Calculate logical resolution (physical / DPI scale)
set /a "LOG_W=MG_W * 100 / MG_SCALE"
set /a "LOG_H=MG_H * 100 / MG_SCALE - MG_TB"
if !LOG_H! lss 600 set LOG_H=600

:: Calculate optimal grid: cols x rows to best fit MG_TOTAL windows
:: Strategy: try cols from 1 upward, pick the first where cols*rows >= total
::           and the cell aspect ratio is closest to 16:9
if !MG_TOTAL! equ 1 (
    set "BEST_C=1"
    set "BEST_R=1"
) else if !MG_TOTAL! leq 2 (
    :: 2 windows: prefer side-by-side if wide enough, else stack
    set /a "half_w=LOG_W / 2"
    if !half_w! geq !MIN_WIN_W! (
        set "BEST_C=2"
        set "BEST_R=1"
    ) else (
        set "BEST_C=1"
        set "BEST_R=2"
    )
) else if !MG_TOTAL! leq 4 (
    set "BEST_C=2"
    set /a "BEST_R=(MG_TOTAL + 1) / 2"
) else if !MG_TOTAL! leq 6 (
    set "BEST_C=3"
    set /a "BEST_R=(MG_TOTAL + 2) / 3"
) else (
    set "BEST_C=4"
    set /a "BEST_R=(MG_TOTAL + 3) / 4"
)

set "MON_%MG%_GRID_COLS=!BEST_C!"
set "MON_%MG%_GRID_ROWS=!BEST_R!"

:: Calculate window size: try DEFAULT, shrink if needed, clamp to MIN
set /a "CELL_W=LOG_W / BEST_C"
set /a "CELL_H=LOG_H / BEST_R"

set "WIN_W=!DEFAULT_WIN_W!"
set "WIN_H=!DEFAULT_WIN_H!"
if !WIN_W! gtr !CELL_W! set "WIN_W=!CELL_W!"
if !WIN_H! gtr !CELL_H! set "WIN_H=!CELL_H!"
if !WIN_W! lss !MIN_WIN_W! set "WIN_W=!MIN_WIN_W!"
if !WIN_H! lss !MIN_WIN_H! set "WIN_H=!MIN_WIN_H!"
set "MON_%MG%_WIN_W=!WIN_W!"
set "MON_%MG%_WIN_H=!WIN_H!"

:: Calculate step (distance between window origins) and safe span
set /a "SAFE_X=LOG_W - WIN_W"
if !SAFE_X! lss 0 set SAFE_X=0
set /a "SAFE_Y=LOG_H - WIN_H"
if !SAFE_Y! lss 0 set SAFE_Y=0

set "STEP_X=!WIN_W!"
if !BEST_C! gtr 1 (
    set /a "CALC_STEP=SAFE_X / (BEST_C - 1)"
    if !CALC_STEP! lss !STEP_X! set "STEP_X=!CALC_STEP!"
)

set "STEP_Y=!WIN_H!"
if !BEST_R! gtr 1 (
    set /a "CALC_STEP=SAFE_Y / (BEST_R - 1)"
    if !CALC_STEP! lss !STEP_Y! set "STEP_Y=!CALC_STEP!"
)

set "MON_%MG%_STEP_X=!STEP_X!"
set "MON_%MG%_STEP_Y=!STEP_Y!"
set "MON_%MG%_SAFE_X=!SAFE_X!"
set "MON_%MG%_SAFE_Y=!SAFE_Y!"

echo [Info] Monitor !MG!: !LOG_W!x!LOG_H! logical, !MG_TOTAL! window^(s^), grid !BEST_C!x!BEST_R!, win !WIN_W!x!WIN_H!, step !STEP_X!x!STEP_Y!
exit /b
