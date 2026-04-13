# multi_play_d2r

> **Diablo II: Resurrected 多开启动器** — 在同一台 Windows 机器上同时启动多个 D2R 游戏实例，并自动完成窗口布局管理。

---

## ✨ 功能特性

- **最多支持 8 个账号同时启动**，每个账号可独立启用/禁用
- **独立的画质和快捷键配置**：脚本在启动时会通过临时接管 `%USERPROFILE%` 的方式，为每个游戏实例分配独立的配置保存目录。实现不同账号拥有不同画质、音量、刷新率和快捷键设定，互不干扰。（首次启动时会自动从系统继承一份你的老配置）
- **多显示器智能布局**（1-4 个显示器灵活适配）：
  - 每个显示器独立配置分辨率、DPI 缩放、坐标偏移和任务栏高度
  - 未指定显示器的账号自动均匀分配到窗口最少的屏幕
  - 每个显示器根据分配到的窗口数自动计算最优网格，窗口尽量平铺、减少堆叠
  - 窗口大小自适应：空间不足时自动缩小（最小 800×600）
- **配置与密码分离**：账号密码单独存放在 `config/accounts_secrets.bat`（已加入 `.gitignore`，不会上传到 GitHub）
- **每个账号可独立配置**：Mod 名称、启动参数（如 `-txt -ns -lq`）、游戏可执行文件路径（`ACCOUNT_x_DIABLO`，留空则使用全局 `diablo`）
- **自动清理残留句柄**：使用 `handle.exe` 查找并关闭残留的游戏进程句柄，避免冲突
- **窗口自动定位**：使用 `NewTitle.exe` 将每个游戏窗口精确移动到计算好的位置
- **调试模式**：通过 `DEBUG_MODE=1` 开启命令回显，方便排查问题

---

## 📁 项目结构

```
multi_play_d2r/
├── multi_play_d2r.bat              # 主启动脚本
├── config/
│   ├── base_settings.bat           # 本地路径与布局配置（需自行创建，见 .example）
│   ├── base_settings.bat.example   # 配置模板（可提交到 GitHub）
│   ├── accounts_secrets.bat        # 账号密码（已被 .gitignore 忽略，切勿上传！）
│   └── accounts_secrets.bat.example # 账号配置模板（可提交到 GitHub）
└── .gitignore
```

---

## 🔧 依赖工具

| 工具 | 用途 | 下载地址 |
|------|------|----------|
| `D2R.exe` | Diablo II: Resurrected 游戏本体 | 通过 Battle.net 安装 |
| `handle.exe` | 查找/关闭残留游戏句柄 | [Sysinternals Handle](https://learn.microsoft.com/zh-cn/sysinternals/downloads/handle) |
| `NewTitle.exe` | 按窗口标题移动/调整窗口大小 | 社区工具 |

> 请将 `handle.exe` 和 `NewTitle.exe` 放置到 `base_settings.bat` 中 `workdir` 所指定的目录下。

---

## ⚙️ 配置说明

### 1. 配置 `config/base_settings.bat`

复制 `config/base_settings.bat.example` 并重命名为 `config/base_settings.bat`，根据实际环境修改：

```bat
:: 启动间隔（秒）
set secs=8

:: 服务器地址
set addres="kr.actual.battle.net"

:: 游戏主程序路径（全局默认，可被每账号 ACCOUNT_x_DIABLO 覆盖）
set diablo="D:\BlizzardGame\d2r\D2R.exe"

:: 工具所在目录（存放 handle.exe 和 NewTitle.exe）
set workdir=D:\BlizzardGame\d2r\bootdiablo

:: 显示器编号（与 Windows 屏幕设置中的编号一致，可跳号）
set MONITOR_IDS=1 2

:: 默认窗口大小和最小窗口大小
set DEFAULT_WIN_W=1280
set DEFAULT_WIN_H=720
set MIN_WIN_W=800
set MIN_WIN_H=600

:: Display 1 — 主显示器
set MON_1_W=3840
set MON_1_H=2160
set MON_1_SCALE=150
set MON_1_X=0
set MON_1_Y=0
set MON_1_TASKBAR=48

:: Display 2 — 外接显示器
set MON_2_W=2560
set MON_2_H=1440
set MON_2_SCALE=100
set MON_2_X=2560
set MON_2_Y=0
set MON_2_TASKBAR=0
```

### 2. 配置 `config/accounts_secrets.bat`

复制 `config/accounts_secrets.bat.example` 并重命名为 `config/accounts_secrets.bat`，填入真实账号信息：

```bat
:: 账号 1
set ACCOUNT_1_ENABLE=1                  :: 1=启用, 0=禁用
set ACCOUNT_1_MONITOR=2                 :: 目标显示器编号（留空则自动分配到最空闲的）
set ACCOUNT_1_USER=your@email.com       :: 战网账号
set ACCOUNT_1_PASS=your_password        :: 游戏密码
set ACCOUNT_1_MOD=tiny                  :: Mod 名称（留空则不加载 Mod）
set ACCOUNT_1_OPTIONS=-txt              :: 可选启动参数
set ACCOUNT_1_DIABLO=                   :: 独立游戏路径（留空则使用全局 diablo）
set ACCOUNT_1_PRIMARY=1                 :: 1=游玩主窗口（大窗口），0=凑人数窗口

:: ... 最多配置到 Account 8
```

> ⚠️ **警告**：`accounts_secrets.bat` 已被 `.gitignore` 忽略，**绝不能上传到 GitHub！**

---

## 🚀 使用方法

1. 按上述说明完成两个配置文件的设置
2. 将工具文件（`handle.exe`、`NewTitle.exe`）放到 `workdir` 指定目录
3. **以管理员身份运行** `multi_play_d2r.bat`
4. 脚本会依次：
   - 检查并关闭残留句柄
   - 启动 D2R 实例
   - 等待 `secs` 秒后调整窗口位置
   - 重复以上步骤，直到所有启用的账号都启动完毕

---

## 🖥️ 窗口布局逻辑

脚本根据每个显示器的实际情况自动计算最优布局：

1. **显示器配置**：用户在 `base_settings.bat` 中用 `MONITOR_IDS` 列出 Windows 屏幕编号（与「设置 > 系统 > 屏幕」一致），每个显示器独立设置分辨率、缩放比、坐标和任务栏高度
2. **账号分配**：
   - 用户可通过 `ACCOUNT_x_MONITOR` 手动指定 Windows 屏幕编号
   - 留空的账号自动分配到当前窗口最少的显示器（均匀分散）
3. **网格计算**：每个显示器根据分配到的窗口数量自动选择最优的 列×行 网格
4. **窗口大小自适应**：默认 1280×720，空间不足时自动缩小，最小 800×600
5. **平铺优先**：窗口间距自动计算，尽量铺满可用空间，减少重叠

| 窗口数 | 自动网格 | 说明 |
|--------|---------|------|
| 1 | 1×1 | 左上角单窗口 |
| 2 | 2×1 | 左右并排 |
| 3-4 | 2×2 | 四宫格（3 个时空一格）|
| 5-6 | 3×2 | 六宫格 |
| 7-8 | 4×2 | 八宫格 |

网格列数由 `GRID_COLS` 控制，超出一行则自动换行。

---

## 🔐 安全说明

| 文件 | 是否提交 GitHub | 说明 |
|------|---------------|------|
| `multi_play_d2r.bat` | ✅ 可以 | 不含敏感信息 |
| `config/base_settings.bat.example` | ✅ 可以 | 仅为模板 |
| `config/accounts_secrets.bat.example` | ✅ 可以 | 仅为模板（含示例占位符） |
| `config/base_settings.bat` | ❌ 不要 | 含本地路径（已加入 .gitignore） |
| `config/accounts_secrets.bat` | ❌ 绝不要 | 含真实账号密码（已加入 .gitignore） |
| `multi_play_d2r_origin.bat` | ❌ 绝不要 | 历史脚本，含真实账号信息（已加入 .gitignore） |

---

## 📝 调试技巧

在 `multi_play_d2r.bat` 顶部将 `DEBUG_MODE` 改为 `1`，可以开启命令回显：

```bat
set DEBUG_MODE=1
```

---

## 📜 许可

本项目仅供个人学习交流，请遵守游戏服务条款。
