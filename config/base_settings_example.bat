@echo off
rem -------------------------------------------------------
rem Diablo II: Resurrected 多开脚本基础设置示例
rem 将此文件复制为 base_settings.bat 并根据实际环境修改。
rem -------------------------------------------------------

rem 切换到游戏所在的盘符与目录（如不需要可留空）。
set "game_drive=D:"
set "game_dir=D:\BlizzardGame\d2r\bootdiablo"

rem 可执行程序及辅助工具路径。
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
