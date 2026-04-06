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
call "config\base_settings.bat"
call "config\accounts_secrets.bat"

:: Change to Working Directory
D:
cd "%workdir%"

echo =========================================================
echo [Info] Starting D2R Multi-Play Launcher...
echo =========================================================

set LAUNCHED_COUNT=0
:: 双显示器布局计数器：Monitor 1 和 Monitor 2
set MON1_COUNT=0
set MON2_COUNT=0

:: Pre-count Enabled Instances for Auto Layout
set TOTAL_MON_1=0
set TOTAL_MON_2=0
if "%AUTO_LAYOUT_GRID%"=="1" (
    for %%I in (1 2 3 4 5 6 7 8) do (
        set "CHK_ENABLE=!ACCOUNT_%%I_ENABLE!"
        set "CHK_ENABLE=!CHK_ENABLE:~0,1!"
        if "!CHK_ENABLE!"=="1" (
            set "CHK_MON=!ACCOUNT_%%I_MONITOR!"
            if "!CHK_MON!"=="" set "CHK_MON=1"
            set "CHK_MON=!CHK_MON:~0,1!"
            if "!CHK_MON!"=="1" (
                set /a TOTAL_MON_1+=1
            ) else (
                set /a TOTAL_MON_2+=1
            )
        )
    )
    
    :: Calculate Auto Grid Cols and Rows for Monitor 1
    if !TOTAL_MON_1! leq 2 (
        set GRID_COLS_MON1=!TOTAL_MON_1!
        set GRID_ROWS_MON1=1
    ) else if !TOTAL_MON_1! leq 4 (
        set GRID_COLS_MON1=2
        set GRID_ROWS_MON1=2
    ) else if !TOTAL_MON_1! leq 6 (
        set GRID_COLS_MON1=3
        set GRID_ROWS_MON1=2
    ) else (
        set GRID_COLS_MON1=4
        set GRID_ROWS_MON1=2
    )
    if !GRID_COLS_MON1! equ 0 set GRID_COLS_MON1=1
    
    :: Calculate Auto Grid Cols and Rows for Monitor 2
    if !TOTAL_MON_2! leq 2 (
        set GRID_COLS_MON2=!TOTAL_MON_2!
        set GRID_ROWS_MON2=1
    ) else if !TOTAL_MON_2! leq 4 (
        set GRID_COLS_MON2=2
        set GRID_ROWS_MON2=2
    ) else if !TOTAL_MON_2! leq 6 (
        set GRID_COLS_MON2=3
        set GRID_ROWS_MON2=2
    ) else (
        set GRID_COLS_MON2=4
        set GRID_ROWS_MON2=2
    )
    if !GRID_COLS_MON2! equ 0 set GRID_COLS_MON2=1
) else (
    set GRID_COLS_MON1=!GRID_COLS!
    set GRID_ROWS_MON1=!GRID_ROWS!
    set GRID_COLS_MON2=!GRID_COLS!
    set GRID_ROWS_MON2=!GRID_ROWS!
)

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
:: Assigned to Monitor 1 -> Monitor 1 (from top-left)
:: Assigned to Monitor 2 -> Monitor 2 (from bottom-left)
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

:: Robust check: only take the first character to avoid trailing whitespace/CR issues
set "ENABLE_FLAG=!ACCT_ENABLE:~0,1!"
if not "!ENABLE_FLAG!"=="1" (
    echo [Info] Account %ACCT_ID% is disabled. Skipping...
    exit /b
)

:: Window size for all instances
set pos_w=1280
set pos_h=720

:: Windows DPI Scaling Adjustment
if not defined DISPLAY1_SCALE set DISPLAY1_SCALE=100
if not defined DISPLAY2_SCALE set DISPLAY2_SCALE=100

:: Calculate actual logical boundaries / 逻辑分辨率边界
set /a LOGICAL_W1=SCREEN_W * 100 / DISPLAY1_SCALE
set /a LOGICAL_H1=SCREEN_H * 100 / DISPLAY1_SCALE
set /a LOGICAL_W2=SCREEN_W * 100 / DISPLAY2_SCALE
set /a LOGICAL_H2=SCREEN_H * 100 / DISPLAY2_SCALE

:: Check if TASKBAR_H is defined, default to 0
if not defined TASKBAR_H set TASKBAR_H=0

:: ====== Monitor 1 Rules ======
set /a "safe_span_x1=LOGICAL_W1 - pos_w"
if !safe_span_x1! lss 0 set safe_span_x1=0
set step_x1=!pos_w!
if defined GRID_COLS_MON1 (
    if !GRID_COLS_MON1! gtr 1 (
        set /a "calc_step_x1=safe_span_x1 / (GRID_COLS_MON1 - 1)"
        if !calc_step_x1! lss !step_x1! set "step_x1=!calc_step_x1!"
    )
)

set /a "safe_span_y1=LOGICAL_H1 - pos_h - TASKBAR_H"
if !safe_span_y1! lss 0 set safe_span_y1=0
set step_y1=!pos_h!
if defined GRID_ROWS_MON1 (
    if !GRID_ROWS_MON1! gtr 1 (
        set /a "calc_step_y1=safe_span_y1 / (GRID_ROWS_MON1 - 1)"
        if !calc_step_y1! lss !step_y1! set "step_y1=!calc_step_y1!"
    )
)

:: ====== Monitor 2 Rules ======
set /a "safe_span_x2=LOGICAL_W2 - pos_w"
if !safe_span_x2! lss 0 set safe_span_x2=0
set step_x2=!pos_w!
if defined GRID_COLS_MON2 (
    if !GRID_COLS_MON2! gtr 1 (
        set /a "calc_step_x2=safe_span_x2 / (GRID_COLS_MON2 - 1)"
        if !calc_step_x2! lss !step_x2! set "step_x2=!calc_step_x2!"
    )
)

set /a "safe_span_y2=LOGICAL_H2 - pos_h"
if !safe_span_y2! lss 0 set safe_span_y2=0
set step_y2=!pos_h!
if defined GRID_ROWS_MON2 (
    if !GRID_ROWS_MON2! gtr 1 (
        set /a "calc_step_y2=safe_span_y2 / (GRID_ROWS_MON2 - 1)"
        if !calc_step_y2! lss !step_y2! set "step_y2=!calc_step_y2!"
    )
)

:: Determine window position based on monitor assignment
if "!ACCT_MONITOR!"=="" set "ACCT_MONITOR=1"
set "MON_CHECK=!ACCT_MONITOR:~0,1!"

if "!MON_CHECK!"=="1" (
    rem Monitor 1, from top-left
    set /a "col=MON1_COUNT %% GRID_COLS_MON1"
    set /a "row=MON1_COUNT / GRID_COLS_MON1"
    set /a "pos_x=col * step_x1"
    set /a "pos_y=row * step_y1"
    
    rem Force bounds check so windows never cross screen boundaries
    if !pos_x! gtr !safe_span_x1! set "pos_x=!safe_span_x1!"
    if !pos_y! gtr !safe_span_y1! set "pos_y=!safe_span_y1!"
    
    set /a MON1_COUNT+=1
    echo [Info] Account !ACCT_ID! ^(!ACCT_MOD!^) -^> Monitor 1 pos: !pos_x!,!pos_y!
) else (
    rem Monitor 2, from bottom-left
    set /a "col=MON2_COUNT %% GRID_COLS_MON2"
    set /a "row=MON2_COUNT / GRID_COLS_MON2"
    set /a "pos_x=col * step_x2"
    set /a "pos_y_offset=row * step_y2"
    
    rem Force bounds check
    if !pos_x! gtr !safe_span_x2! set "pos_x=!safe_span_x2!"
    if !pos_y_offset! gtr !safe_span_y2! set "pos_y_offset=!safe_span_y2!"
    
    set /a "pos_y=MONITOR2_Y_OFFSET + safe_span_y2 - pos_y_offset"
    
    set /a MON2_COUNT+=1
    echo [Info] Account !ACCT_ID! ^(!ACCT_MOD!^) -^> Monitor 2 pos: !pos_x!,!pos_y!
)

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

:: Start the game using a wrapper command to inject the custom USERPROFILE environment variable
cmd /c "set USERPROFILE=!FAKE_PROFILE! && start "" %diablo% -username !ACCT_USER! -password !ACCT_PASS! -address %addres% -mod !ACCT_MOD! -w !ACCT_OPTIONS!"
echo [Info] Instance !ACCT_ID! launched successfully. Waiting %secs% seconds before adjusting window...
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
