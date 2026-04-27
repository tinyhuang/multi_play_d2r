# D2R Multi-Play Manager

一个面向 Diablo II: Resurrected 的多开启动与管理工具（Windows，.NET 8 WinForms）。

核心目标很直接：

- 让多账号启动更稳定
- 让每个账号配置互不干扰
- 让多显示器窗口排布更可控
- 让日常使用从“命令行脚本”升级到“可视化流程”

## 这个软件解决了哪些痛点

传统批处理多开常见问题：

- 启动顺序靠手工控制，失败时排查成本高
- 多账号共享同一套用户配置，容易互相覆盖
- 多屏窗口位置不稳定，重启后经常错位
- 参数组合依赖记忆，不同账号容易配错
- 日志和版本不可追溯，出问题很难定位

本项目对应的解决方案：

- 使用 handle.exe 合规处理互斥量，提升多开成功率
- 账号级 profile 隔离（独立 USERPROFILE / Settings.json）
- 可视化游戏窗口布局（支持多显示器）
- 账号级启动参数与角色策略（master / slave）
- 配置持久化、导入导出、状态监控与版本标记

## 主要功能

- 账号管理：启用/禁用、角色、邮箱、密码、独立路径
- 启动策略：批量启动、间隔控制、进程守卫
- 服务器配置：全局默认 + 账号级覆盖
- 窗口布局：可视化拖拽与自动网格排列
- 语言切换：中文 / English
- 前置校验：D2R.exe 与 handle.exe 必填高亮提示
- 版本可见：状态栏与 About 显示 Version/Build

## 快速使用

1. 在 Release 页面下载最新版本 zip 并解压运行。
2. 首次打开后先配置两项前置路径：
	 - D2R.exe
	 - handle.exe
3. 添加账号，填写 Battle.net 邮箱与密码。
4. 根据用途设置角色：
	 - master：主玩窗口
	 - slave：低负载挂机窗口
5. 打开“游戏窗口布局”设置每个账号窗口位置。
6. 点击“全部启动”。

## 工作原理（简版）

启动每个实例时，程序会：

1. 为账号准备独立 profile 目录
2. 覆盖 USERPROFILE 构造进程环境块
3. 按账号参数生成命令行（含角色策略）
4. 调用 CreateProcessW 启动 D2R
5. 启动后执行窗口定位与布局修正

这意味着“配置隔离”主要是设置层隔离，不是复制整套游戏目录，因此运行时额外开销通常很小。

## 安装与环境

- 系统：Windows 10/11
- 权限：建议管理员运行（用于 handle.exe 操作）
- 依赖：Sysinternals handle.exe

## 项目结构

```
multi_play_d2r/
├── D2RMultiPlay.sln
├── src/
│   ├── D2RMultiPlay.Core/        # 启动、配置、窗口、互斥处理核心逻辑
│   └── D2RMultiPlay.App/         # WinForms UI
├── tests/
│   └── D2RMultiPlay.Core.Tests/  # 核心单元测试
└── .github/workflows/            # CI 与 Release 工作流
```

## 版本与发布策略

- main 分支：
	- 使用语义化版本 tag（vX.Y.Z）发布正式版
	- tag 触发 Release 工作流，自动生成 GitHub Release 与 zip 包

- feature 分支：
	- CI 产物自动携带预发布版本号
	- 便于回溯每次构建对应的分支与提交

- UI 版本显示：
	- 状态栏显示 Build 信息
	- About 显示 Version + Build

## 合规与安全

- 不注入游戏进程，不修改游戏内存
- 互斥处理基于官方 Sysinternals 工具
- 密码使用 DPAPI（CurrentUser）加密存储
- 请在遵守游戏服务条款的前提下使用

## Git 提交身份约束

本仓库约定只允许使用以下身份提交：

- Name: tinyhuang
- Email: tinyhuang@163.com

仓库内置了 .githooks/pre-commit 钩子用于拦截不符合身份的提交。

首次克隆后建议执行：

```bash
git config core.hooksPath .githooks
git config user.name tinyhuang
git config user.email tinyhuang@163.com
```
