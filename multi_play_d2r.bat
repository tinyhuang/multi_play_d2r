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
:: 双显示器布局计数器：yte mod -> Monitor 1, 其他 mod -> Monitor 2
set YTE_COUNT=0
set OTHER_COUNT=0

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
:: yte mod windows -> Monitor 1 (from top-left)
:: other mod windows -> Monitor 2 (from bottom-left)
:: ==========================================
:CheckAndLaunch
set "ACCT_ID=%~1"

:: Dynamically read account variables using delayed expansion
set "ACCT_ENABLE=!ACCOUNT_%ACCT_ID%_ENABLE!"
set "ACCT_USER=!ACCOUNT_%ACCT_ID%_USER!"
set "ACCT_PASS=!ACCOUNT_%ACCT_ID%_PASS!"
set "ACCT_MOD=!ACCOUNT_%ACCT_ID%_MOD!"
set "ACCT_OPTIONS=!ACCOUNT_%ACCT_ID%_OPTIONS!"

:: Robust check: only take the first character to avoid trailing whitespace/CR issues
set "ENABLE_FLAG=!ACCT_ENABLE:~0,1!"
if not "!ENABLE_FLAG!"=="1" (
    echo [Info] Account %ACCT_ID% is disabled. Skipping...
    exit /b
)

:: Window size for all instances
set pos_w=1280
set pos_h=720

:: Determine window position based on mod type
set "MOD_CHECK=!ACCT_MOD!"
:: Trim potential trailing whitespace/CR from mod name
for %%m in (!MOD_CHECK!) do set "MOD_CHECK=%%m"

if /i "!MOD_CHECK!"=="yte" (
    rem yte mod -> Monitor 1, from top-left
    set /a "col=YTE_COUNT %% GRID_COLS"
    set /a "row=YTE_COUNT / GRID_COLS"
    set /a "pos_x=col * pos_w"
    set /a "pos_y=row * pos_h"
    set /a YTE_COUNT+=1
    echo [Info] Account %ACCT_ID% ^(yte^) -^> Monitor 1 pos: !pos_x!,!pos_y!
) else (
    rem other mods -> Monitor 2, from bottom-left
    set /a "col=OTHER_COUNT %% GRID_COLS"
    set /a "row=OTHER_COUNT / GRID_COLS"
    set /a "pos_x=col * pos_w"
    set /a "pos_y=MONITOR2_Y_OFFSET + (SCREEN_H - pos_h) - row * pos_h"
    set /a OTHER_COUNT+=1
    echo [Info] Account %ACCT_ID% ^(!ACCT_MOD!^) -^> Monitor 2 pos: !pos_x!,!pos_y!
)

:: Define a separate profile directory for this account to isolate Settings
set "FAKE_PROFILE=%workdir%\profiles\account_!ACCT_ID!"
set "D2R_SAVE_PATH=!FAKE_PROFILE!\Saved Games\Diablo II Resurrected"

:: Create the faux profile directory if it does not exist
if not exist "!D2R_SAVE_PATH!" (
    mkdir "!D2R_SAVE_PATH!"
    
    :: Attempt to copy the system's default Settings.json as a base configuration
    if exist "%USERPROFILE%\Saved Games\Diablo II Resurrected\Settings.json" (
        echo [Info] Copying default Settings.json for Account !ACCT_ID!...
        copy "%USERPROFILE%\Saved Games\Diablo II Resurrected\Settings.json" "!D2R_SAVE_PATH!\Settings.json" >nul
    )
)

echo [Info] Checking handles for Instance !ACCT_ID!...
%myhandler% -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y

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
