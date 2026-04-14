@echo off
rem -------------------------------------------------------
rem Diablo II: Resurrected 多开脚本基础设置示例
rem 将此文件复制为 base_settings.bat 并根据实际环境修改。
rem -------------------------------------------------------

rem 切换到游戏所在的盘符与目录（如不需要可留空）。
set "game_drive=D:"
set "game_dir=D:\BlizzardGame\d2r\bootdiablo"

rem 可执行程序及辅助工具路径。
rem diablo 为默认启动程序；可在 multi_play.bat 中用 ACC*_diablo 对单个账号覆盖。
set "diablo=D:\BlizzardGame\d2r\D2R.exe"
set "myhandler=D:\BlizzardGame\d2r\bootdiablo\handle.exe"
set "newtitle=D:\BlizzardGame\d2r\bootdiablo\NewTitle.exe"

rem 服务器与等待时间。
set "addres=kr.actual.battle.net"
set "secs=3"

rem 显示器与窗口布局设置（默认 4K 屏 + 1280x720 网格）。
set "display_width=3840"
set "display_height=2160"
set "default_windowW=1280"
set "default_windowH=720"
set "layout_offsetX=0"
set "layout_offsetY=0"
set "layout_priority=0,0 1,0 2,0 0,1 1,1 2,1 0,2 1,2 2,2"
set "layout_priority_with_mdk=0,0 1,0 2,0 0,1 2,1 0,2 2,2 1,1 1,2"
set "mdk_windowW=2560"
set "mdk_windowH=1440"
set "mdk_bottom_margin=0"

rem 默认 mod、启动顺序、同开数量限制、统一启动参数。
set "default_mod=tiny"
set "account_order=ACC1 ACC2 ACC3 ACC4 ACC5 ACC6 ACC7 ACC8"
set "max_accounts=8"
set "default_launch_args=-w -ns -lowquality -skiptobnet -txt"

rem -------------------------------------------------------
rem 账号与每账号配置（推荐全部在本文件维护）
rem 变量含义：
rem  - ACC*_username / ACC*_password: 账号密码
rem  - ACC*_enabled: 1 启用，0 跳过
rem  - ACC*_mod: 留空则使用 default_mod
rem  - ACC*_diablo: 留空则使用全局 diablo
rem  - ACC*_launchArgs: 附加启动参数
rem  - ACC*_windowName: 窗口标题关键字（用于移动窗口）
rem  - ACC*_message: 启动后提示信息（可留空）
rem  - ACC*_windowX/Y/W/H: 可选，留空走自动布局
rem -------------------------------------------------------

set "ACC1_username=you@example.com"
set "ACC1_password=change_me"
set "ACC1_enabled=1"
set "ACC1_mod="
set "ACC1_diablo="
set "ACC1_launchArgs="
set "ACC1_windowName=one"
set "ACC1_message=账号1启动完成"

set "ACC2_username=you@example.com"
set "ACC2_password=change_me"
set "ACC2_enabled=1"
set "ACC2_mod=MDK"
set "ACC2_diablo="
set "ACC2_launchArgs="
set "ACC2_windowName=two"
set "ACC2_message=账号2启动完成"

set "ACC3_username=you@example.com"
set "ACC3_password=change_me"
set "ACC3_enabled=1"
set "ACC3_mod="
set "ACC3_diablo="
set "ACC3_launchArgs="
set "ACC3_windowName=three"
set "ACC3_message=账号3启动完成"

set "ACC4_username=you@example.com"
set "ACC4_password=change_me"
set "ACC4_enabled=1"
set "ACC4_mod="
set "ACC4_diablo="
set "ACC4_launchArgs="
set "ACC4_windowName=four"
set "ACC4_message=账号4启动完成"

set "ACC5_username=you@example.com"
set "ACC5_password=change_me"
set "ACC5_enabled=1"
set "ACC5_mod="
set "ACC5_diablo="
set "ACC5_launchArgs="
set "ACC5_windowName=five"
set "ACC5_message=账号5启动完成"

set "ACC6_username=you@example.com"
set "ACC6_password=change_me"
set "ACC6_enabled=1"
set "ACC6_mod="
set "ACC6_diablo="
set "ACC6_launchArgs="
set "ACC6_windowName=six"
set "ACC6_message=账号6启动完成"

set "ACC7_username=you@example.com"
set "ACC7_password=change_me"
set "ACC7_enabled=1"
set "ACC7_mod="
set "ACC7_diablo="
set "ACC7_launchArgs="
set "ACC7_windowName=seven"
set "ACC7_message=账号7启动完成"

set "ACC8_username=you@example.com"
set "ACC8_password=change_me"
set "ACC8_enabled=1"
set "ACC8_mod="
set "ACC8_diablo="
set "ACC8_launchArgs="
set "ACC8_windowName=eight"
set "ACC8_message=账号8启动完成"
