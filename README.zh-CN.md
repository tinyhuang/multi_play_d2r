# D2R 多开管理器

[English](README.md) | [中文](README.zh-CN.md)

这是一个给 Diablo II: Resurrected 用的多开管理工具（Windows）。

一句话介绍：
- 多开更稳
- 每个账号配置互不干扰
- 多屏窗口更好摆
- 日常操作比老 BAT 脚本省心很多

## 它主要解决什么问题？

如果你用过旧的批处理多开，大概率遇到过这些坑：
- 启动顺序容易翻车，排查很痛苦
- 多账号共用配置，经常互相覆盖
- 窗口位置重启后乱跑
- 启动参数要靠记忆，容易配错
- 出问题时看不到明确版本，难回溯

这个工具对应给你做了这些：
- 用 handle.exe 做合规互斥量处理
- 账号级 profile 隔离（独立 USERPROFILE / Settings.json）
- 多显示器可视化窗口布局
- master/slave 角色化启动策略
- 配置导入导出、持久化、版本可见

## 快速上手

1. 去 Release 下载最新 zip。
2. 打开程序后先配两个必填路径：
   - D2R.exe
   - handle.exe
3. 添加 Battle.net 账号。
4. 需要省资源时，把角色设为 slave。
5. 在“游戏窗口布局”里摆好窗口。
6. 点击“全部启动”。

## 重要说明

- handle.exe 是必填项，不配很容易导致多开失败。
- 当前只在国际版完成测试。
- 国服版如有明确需求，再评估后续支持。

## 构建与发布

- `main`：通过 tag（`vX.Y.Z`）发布正式版
- `feature/**`：生成带分支标识的预发布构建
- UI 会显示 Version + Build，方便定位问题

## 目录结构

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

## 合规说明

- 不注入游戏内存
- 不改游戏二进制
- 互斥处理使用官方 Sysinternals 工具
