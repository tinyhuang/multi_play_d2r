# D2R Multi-Play Manager

[English](README.md) | [中文](README.zh-CN.md)

This tool helps you launch and manage multiple Diablo II: Resurrected accounts on Windows.

Short version:
- Fewer failed multi-launch attempts
- Cleaner per-account setup
- Better multi-monitor window control
- Easier day-to-day use than old batch scripts

## What pain does it solve?

If you used old BAT-based workflows, you probably hit these:
- Launch order is fragile and hard to debug
- Accounts overwrite each other's settings
- Windows jump to random positions on reboot
- Parameter combos are easy to mix up
- No clear build/version trace when something breaks

This app fixes that with:
- Sysinternals handle.exe based mutex handling
- Per-account profile isolation (separate USERPROFILE / Settings.json)
- Visual window layout editor for multi-monitor setups
- Role-based launch strategy (master/slave)
- Import/export, persisted config, and visible build metadata

## Quick Start

1. Download the latest zip from Releases.
2. Open the app and configure two required paths first:
   - D2R.exe
   - handle.exe
3. Add your Battle.net accounts.
4. Set role to slave for lower load where needed.
5. Arrange windows in Game Window Layout.
6. Click Launch All.

## Important Notes

- handle.exe is required. If you skip it, multi-launch can fail.
- Current testing is for the international version only.
- China region support can be considered later when there is clear demand.

## Build & Release

- `main`: release by tag (`vX.Y.Z`)
- `feature/**`: preview artifacts with branch-aware versioning
- UI shows Version + Build for easier troubleshooting

## Project Layout

```text
multi_play_d2r/
├── D2RMultiPlay.sln
├── src/
│   ├── D2RMultiPlay.Core/
│   └── D2RMultiPlay.App/
├── tests/
│   └── D2RMultiPlay.Core.Tests/
└── .github/workflows/
```

## Compliance

- No memory injection
- No game binary patching
- Mutex handling is based on official Sysinternals tooling
