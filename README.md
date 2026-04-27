# multi_play_d2r

Diablo II: Resurrected 多开配置与进程管理器（重构版，.NET 8 / WinForms）。

本分支已从旧版 BAT 脚本迁移为 Windows 桌面应用架构，核心目标：

- 合规多开（调用 Sysinternals handle.exe 处理互斥量）
- 每账号独立配置环境（隔离 USERPROFILE / Settings.json）
- 多显示器可视化布局与窗口控制
- 配置持久化、导入导出与进程守卫

## 当前目录结构

```
multi_play_d2r/
├── D2RMultiPlay.sln
├── Directory.Build.props
├── src/
│   ├── D2RMultiPlay.Core/
│   └── D2RMultiPlay.App/
├── tests/
│   └── D2RMultiPlay.Core.Tests/
└── .github/workflows/build.yml
```

## 关键说明

- 目标运行环境是 Windows（建议 Windows 10/11）。
- UI 应用在 manifest 中声明 `requireAdministrator`，用于 handle.exe 合规清理互斥量句柄。
- 不再依赖旧版 `multi_play_d2r.bat`、`config/*.bat.example`、`NewTitle.exe`。
- 账号敏感信息采用 DPAPI（CurrentUser）存储。

## 构建与发布

本仓库已配置 GitHub Actions（windows-latest）进行：

- `dotnet restore`
- `dotnet build`
- `dotnet test`
- `dotnet publish`（Native AOT, win-x64）

## 版本与发布策略（2026-04 起）

- 主分支 `main`：
	- 使用语义化版本 Tag（`vX.Y.Z`）作为正式发布。
	- 推送 Tag 会触发 `Release` 工作流，生成 GitHub Release 与 zip 安装包。

- 非主分支（`feature/**`）：
	- CI 自动生成预发布版本号：`X.Y.Z-<branch>.<run_number>`。
	- 构建产物名携带版本号，便于回溯。

- UI 显示：
	- 主界面状态栏显示 `Build: <InformationalVersion>`。
	- About 对话框显示 `Version` 与 `Build`，用于用户报障与回滚定位。

- 统一约定：
	- 无论哪个分支，发布产物都带版本标记。
	- 正式对外发布只建议在 `main` 上通过 Tag 完成。

## 合规与安全

- 不注入游戏进程，不修改游戏内存。
- 互斥量处理基于官方 Sysinternals 工具。
- 项目仅用于个人学习与工程实践，请遵守游戏服务条款。
