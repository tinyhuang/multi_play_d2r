@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "script_dir=%~dp0"

rem -------------------基础设置加载------------------------
if defined D2R_BASE_CONFIG (
    set "base_config=%D2R_BASE_CONFIG%"
) else (
    set "base_config=%script_dir%config\base_settings.bat"
)
if not exist "%base_config%" (
    echo [ERROR] 未找到基础设置文件: %base_config%
    echo 请创建 config\base_settings.bat（可参考 config\base_settings_example.bat）并填入基础配置。
    exit /b 1
)
call "%base_config%"
rem -------------------------------------------------------------------------------

if defined game_drive (
    %game_drive%
)
if defined game_dir (
    cd "%game_dir%"
)

echo 现在开始运行 ========================================

rem -------------------------------------------------------
rem 启动顺序及默认 mod 设置
rem  - 默认只需要维护 config\base_settings.bat（含全局 + 每账号参数 + 账号密码）
rem  - 如需兼容旧版本，可保留 config\accounts_secrets.bat 作为账号密码回退来源
rem  - 调整 account_order 即可改变启动顺序
rem  - 将 *_enabled 设为 0 可以跳过对应账号
rem  - 留空 *_mod 表示使用 default_mod，单独填值即可指定 mod
rem  - ACC1= 大号亚马逊， ACC2=大号法师圣骑士死灵， ACC3=刺客，打钱野蛮人
rem  - ACC4= 火焰德鲁伊， ACC5=buff野蛮人， 
rem  - ACC6=占位法师， ACC7=占位法师， ACC8=占位死灵
rem  - 可用命令行参数或 D2R_ACCOUNTS 环境变量覆盖 account_order（最大 8 个）
rem  - 基础设置集中在 config\base_settings.bat 中，修改该文件即可
rem  - 窗口位置/大小默认根据 base_settings 中的布局规则自动分配；需要自定义时在 base_settings 中写 ACC*_windowX/Y/W/H
rem  - 可为每个账号单独指定启动程序路径：ACC*_diablo（留空则使用全局 diablo）
rem -------------------------------------------------------

rem -------------------旧版账号凭证回退（可选）------------------------
if defined D2R_ACCOUNTS_FILE (
    set "accounts_file=%D2R_ACCOUNTS_FILE%"
) else (
    set "accounts_file=%script_dir%config\accounts_secrets.bat"
)
set "legacy_accounts_loaded=0"
if exist "%accounts_file%" (
    call "%accounts_file%"
    set "legacy_accounts_loaded=1"
)
call :ApplyAccountDefaults ACC1 1
call :ApplyAccountDefaults ACC2 2
call :ApplyAccountDefaults ACC3 3
call :ApplyAccountDefaults ACC4 4
call :ApplyAccountDefaults ACC5 5
call :ApplyAccountDefaults ACC6 6
call :ApplyAccountDefaults ACC7 7
call :ApplyAccountDefaults ACC8 8

if "%legacy_accounts_loaded%"=="1" (
    set "legacy_notice_needed=0"
    for %%A in (ACC1 ACC2 ACC3 ACC4 ACC5 ACC6 ACC7 ACC8) do (
        call set "tmp_user=%%%A_username%%"
        call set "tmp_pass=%%%A_password%%"
        if defined tmp_user if defined tmp_pass set "legacy_notice_needed=1"
    )
    if "!legacy_notice_needed!"=="1" (
        echo [INFO] 检测到旧版账号配置文件: %accounts_file%
        echo [INFO] 建议将 ACC*_username / ACC*_password 迁移到 config\base_settings.bat，后续仅维护一个文件。
    )
)

rem -------------------账号选择逻辑------------------------
rem 优先顺序：命令行参数 > D2R_ACCOUNTS 环境变量 > account_order
set "selected_accounts=%account_order%"
if defined D2R_ACCOUNTS (
    set "selected_accounts=%D2R_ACCOUNTS%"
)
if not "%~1"=="" (
    set "selected_accounts=%*"
)

if not defined selected_accounts (
    echo [ERROR] 未指定任何账号，退出。
    exit /b 1
)

set /a account_count=0
set "accounts_to_launch="
for %%A in (%selected_accounts%) do (
    set /a account_count+=1
    if !account_count! GTR %max_accounts% (
        echo [ERROR] 账号数量超过最大限制（%max_accounts%）。请减少需要启动的账号数。
        exit /b 1
    )
    if not defined accounts_to_launch (
        set "accounts_to_launch=%%A"
    ) else (
        set "accounts_to_launch=!accounts_to_launch! %%A"
    )
)

if !account_count! EQU 0 (
    echo [ERROR] 未指定任何需要启动的账号。
    exit /b 1
)

call :PrepareLayout !accounts_to_launch!

for %%A in (!accounts_to_launch!) do (
    call :LaunchAccount %%A
)

echo 所有已启用的账号都启动完成。
goto :EOF

:PrepareLayout
if "%~1"=="" goto :EOF
set "mdk_required=0"
for %%A in (%*) do (
    call set "prep_enabled=%%%A_enabled%%"
    if /I "!prep_enabled!"=="1" (
        set "prep_mod="
        call set "prep_mod=%%%A_mod%%"
        if /I "!prep_mod!"=="MDK" (
            set "mdk_required=1"
        )
    )
)
if not defined default_windowW set "default_windowW=1280"
if not defined default_windowH set "default_windowH=720"
if not defined layout_priority set "layout_priority=0,0 1,0 2,0 0,1 1,1 2,1 0,2 1,2 2,2"
if not defined layout_priority_with_mdk set "layout_priority_with_mdk=0,0 1,0 2,0 0,1 2,1 0,2 2,2 1,1 1,2"
set "active_layout_priority=!layout_priority!"
if "!mdk_required!"=="1" (
    if defined layout_priority_with_mdk (
        set "active_layout_priority=!layout_priority_with_mdk!"
    )
)
if not defined active_layout_priority set "active_layout_priority=0,0 1,0 2,0 0,1 1,1 2,1 0,2 1,2 2,2"
set "remaining_layout=!active_layout_priority!"
set /a layout_index=0
if not defined layout_offsetX set "layout_offsetX=0"
if not defined layout_offsetY set "layout_offsetY=0"
if not defined display_width set "display_width=3840"
if not defined display_height set "display_height=2160"
if not defined mdk_windowW set "mdk_windowW=2560"
if not defined mdk_windowH set "mdk_windowH=1440"
if not defined mdk_bottom_margin set "mdk_bottom_margin=0"
set /a mdk_posX=(display_width - mdk_windowW) / 2
if !mdk_posX! LSS 0 set "mdk_posX=0"
set /a mdk_posY=display_height - mdk_windowH - mdk_bottom_margin
if !mdk_posY! LSS 0 set "mdk_posY=0"
for %%A in (%*) do (
    call :SetupWindowDefaults %%A
)
goto :EOF

:ApplyAccountDefaults
set "account=%~1"
set "index=%~2"
if "%account%"=="" goto :EOF

call set "tmp=%%%account%_enabled%%"
if not defined tmp call set "%account%_enabled=1"

call set "tmp=%%%account%_mod%%"
if not defined tmp call set "%account%_mod="

call set "tmp=%%%account%_diablo%%"
if not defined tmp call set "%account%_diablo="

call set "tmp=%%%account%_launchArgs%%"
if not defined tmp call set "%account%_launchArgs="

call set "tmp=%%%account%_windowName%%"
if not defined tmp call set "%account%_windowName=ACC%index%"

call set "tmp=%%%account%_message%%"
if not defined tmp call set "%account%_message="

set "tmp="
goto :EOF

:SetupWindowDefaults
set "account=%~1"
if "%account%"=="" goto :EOF
set "prep_enabled="
call set "prep_enabled=%%%account%_enabled%%"
if /I "!prep_enabled!" NEQ "1" goto :EOF
set "prep_mod="
call set "prep_mod=%%%account%_mod%%"
if /I "!prep_mod!"=="MDK" (
    call :EnsureMDKWindow %account%
) else (
    call :EnsureStandardWindow %account%
)
goto :EOF

:EnsureMDKWindow
set "account=%~1"
set "tmp="
call set "tmp=%%%account%_windowW%%"
if not defined tmp (
    call set "%account%_windowW=!mdk_windowW!"
)
set "tmp="
call set "tmp=%%%account%_windowH%%"
if not defined tmp (
    call set "%account%_windowH=!mdk_windowH!"
)
set "tmp="
call set "tmp=%%%account%_windowX%%"
if not defined tmp (
    call set "%account%_windowX=!mdk_posX!"
)
set "tmp="
call set "tmp=%%%account%_windowY%%"
if not defined tmp (
    call set "%account%_windowY=!mdk_posY!"
)
goto :EOF

:EnsureStandardWindow
call :AssignNextLayoutPosition %1
goto :EOF

:AssignNextLayoutPosition
set "account=%~1"
if "%account%"=="" goto :EOF
set "tmp_windowX="
call set "tmp_windowX=%%%account%_windowX%%"
set "tmp_windowY="
call set "tmp_windowY=%%%account%_windowY%%"
set "need_position=1"
if defined tmp_windowX if defined tmp_windowY set "need_position=0"
if "!need_position!"=="1" (
    set "next_cell="
    set "layout_rest="
    if defined remaining_layout (
        for /f "tokens=1* delims= " %%c in ("!remaining_layout!") do (
            set "next_cell=%%c"
            set "layout_rest=%%d"
        )
    )
    set "remaining_layout=!layout_rest!"
    if defined next_cell (
        for /f "tokens=1,2 delims=," %%c in ("!next_cell!") do (
            set "layout_col=%%c"
            set "layout_row=%%d"
        )
        set /a posX=layout_offsetX + layout_col * !default_windowW!
        set /a posY=layout_offsetY + layout_row * !default_windowH!
    ) else (
        set /a posX=layout_offsetX
        set /a posY=layout_offsetY + !layout_index! * !default_windowH!
    )
    set /a layout_index+=1
    call set "%account%_windowX=!posX!"
    call set "%account%_windowY=!posY!"
)
set "tmp_windowW="
call set "tmp_windowW=%%%account%_windowW%%"
if not defined tmp_windowW (
    call set "%account%_windowW=!default_windowW!"
)
set "tmp_windowH="
call set "tmp_windowH=%%%account%_windowH%%"
if not defined tmp_windowH (
    call set "%account%_windowH=!default_windowH!"
)
goto :EOF

:LaunchAccount
set "account=%~1"
call set "enabled=%%%account%_enabled%%"
if /I "!enabled!" NEQ "1" (
    echo 跳过 !account! (未启用)。
    goto :EOF
)

call set "username=%%%account%_username%%"
call set "password=%%%account%_password%%"
if not defined username goto :MissingData
if not defined password goto :MissingData

call set "mod=%%%account%_mod%%"
if not defined mod set "mod=%default_mod%"
call set "windowName=%%%account%_windowName%%"
call set "windowX=%%%account%_windowX%%"
call set "windowY=%%%account%_windowY%%"
call set "windowW=%%%account%_windowW%%"
call set "windowH=%%%account%_windowH%%"
call set "message=%%%account%_message%%"
call set "accountDiablo=%%%account%_diablo%%"
if not defined accountDiablo set "accountDiablo=%diablo%"
if not defined accountDiablo goto :MissingExecutable
if not exist "!accountDiablo!" goto :MissingExecutable
set "accountLaunchArgs="
call set "accountLaunchArgs=%%%account%_launchArgs%%"
set "launchSwitch=%default_launch_args%"
if defined accountLaunchArgs (
    set "launchSwitch=!launchSwitch! !accountLaunchArgs!"
)

call :KillHandles

set "modSwitch="
if defined mod set "modSwitch=-mod !mod!"

echo 启动账号: !username! (mod: !mod!, exe: !accountDiablo!)
start "" "!accountDiablo!" -username !username! -password !password! -address "%addres%" !modSwitch! !launchSwitch!
if defined message echo !message!

timeout /T %secs%
call :PositionWindow "!windowName!" "!windowX!" "!windowY!" "!windowW!" "!windowH!"
goto :EOF

:MissingData
echo !account! 缺少必须的账号或密码，跳过。
goto :EOF

:MissingExecutable
echo !account! 的启动程序路径无效，跳过。
echo 请检查 !account!_diablo 或全局 diablo 是否存在。
goto :EOF

:KillHandles
"%myhandler%" -a "Check For Other Instances" -nobanner > Handle.txt 2>&1
if exist Handle.txt (
    echo Handle 信息：
    type Handle.txt
    for /f "tokens=3,6 delims= " %%a in (Handle.txt) do "%myhandler%" -p %%a -c %%b -y
) else (
    echo 没有发现 Handle 信息。
)
goto :EOF

:PositionWindow
if "%~1"=="" goto :EOF
if "%~2"=="" goto :EOF
"%newtitle%" "%~1" %~2 %~3 %~4 %~5
goto :EOF
