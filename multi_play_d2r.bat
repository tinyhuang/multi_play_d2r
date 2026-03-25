@echo off
:: ==========================================
:: Debug Mode Settings
:: Set to 1 to see all commands printed (useful for tracing errors)
:: ==========================================
set DEBUG_MODE=0
if "%DEBUG_MODE%"=="1" (
    @echo on
)

setlocal
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

:: ==========================================
:: Launch Instance 1
:: ==========================================
echo [Info] Checking handles for Instance 1...
%myhandler% -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start "" %diablo% -username %ACCOUNT_1_USER% -password %ACCOUNT_1_PASS% -address %addres% -mod %ACCOUNT_1_MOD% -txt -w
echo [Info] Instance 1 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "one"  -5 5 871 520

:: ==========================================
:: Launch Instance 2
:: ==========================================
echo [Info] Checking handles for Instance 2...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start "" %diablo% -username %ACCOUNT_2_USER% -password %ACCOUNT_2_PASS% -address %addres% -mod %ACCOUNT_2_MOD% -txt -w
echo [Info] Instance 2 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "two"  506 401 1724 1000

:: ==========================================
:: Launch Instance 3
:: ==========================================
echo [Info] Checking handles for Instance 3...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start ""  %diablo%  -username %ACCOUNT_3_USER%  -password %ACCOUNT_3_PASS% -address %addres% -mod %ACCOUNT_3_MOD% -txt -w
echo [Info] Instance 3 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "three" 849 2 871 520

:: ==========================================
:: Launch Instance 4
:: ==========================================
echo [Info] Checking handles for Instance 4...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start ""  %diablo%  -username %ACCOUNT_4_USER%  -password %ACCOUNT_4_PASS% -address %addres% -mod %ACCOUNT_4_MOD% -txt -w
echo [Info] Instance 4 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "four"  1703 4 871 520

:: ==========================================
:: Launch Instance 5
:: ==========================================
echo [Info] Checking handles for Instance 5...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start ""  %diablo%  -username %ACCOUNT_5_USER%  -password %ACCOUNT_5_PASS% -address %addres% -mod %ACCOUNT_5_MOD% -txt -w
echo [Info] Instance 5 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "five" -6 500 871 520

:: ==========================================
:: Launch Instance 6
:: ==========================================
echo [Info] Checking handles for Instance 6...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start ""  %diablo%  -username %ACCOUNT_6_USER%  -password %ACCOUNT_6_PASS% -address %addres% -mod %ACCOUNT_6_MOD% -txt -w
echo [Info] Instance 6 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "six"  1699 513 871 520

:: ==========================================
:: Launch Instance 7
:: ==========================================
echo [Info] Checking handles for Instance 7...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start ""  %diablo%  -username %ACCOUNT_7_USER%  -password %ACCOUNT_7_PASS% -address %addres% -mod %ACCOUNT_7_MOD% -txt -w
echo [Info] Instance 7 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "seven"  -6 1110 871 520

:: ==========================================
:: Launch Instance 8
:: ==========================================
echo [Info] Checking handles for Instance 8...
%myhandler%  -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
for /f "tokens=3,6 delims= " %%a in (Handle.txt) do handle.exe -p %%a -c %%b -y
start ""  %diablo%  -username %ACCOUNT_8_USER%  -password %ACCOUNT_8_PASS% -address %addres% -mod %ACCOUNT_8_MOD% -txt -w
echo [Info] Instance 8 launched successfully. Waiting %secs% seconds before adjusting window...
timeout /T %secs%
newtitle "eight"  1695 877 871 520

echo =========================================================
echo [Success] All D2R instances have been launched and arranged!
echo =========================================================

:: Wait for user input to prevent the window from closing immediately, useful for debugging
echo.
echo [Info] Script execution finished. Press any key to exit...
pause
endlocal
