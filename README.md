# 电脑伴侣（ComputerCompanion）

电脑伴侣是一款面向 Windows x64 的桌面硬件监控与游戏性能显示工具，基于 .NET 8 和 Avalonia 11.2.0。它采用主进程加悬浮窗子进程架构，提供硬件指标、真实游戏 FPS、告警和数据导出。

## 核心功能

- CPU、GPU、内存、磁盘、网络和电池监控，硬件数据来自 LibreHardwareMonitor。
- 通过 Windows ETW 追踪 DXGI Present 事件，提供真实游戏 FPS、Frame Time 和 1% Low。
- 独立悬浮窗进程，显示 FPS、CPU、GPU、内存等指标，并支持异常恢复。
- 4 套 JSON 皮肤预设：`minimal`、`tech`、`retro`、`night`；可在 `Assets/Skins/` 扩展。
- 温度、占用率等规则告警，支持声音提示。
- CSV、JSON 和 HTML 性能数据导出。
- 系统托盘、全局热键、设置持久化和本地配置备份。

## 环境要求

- Windows 10/11 x64。
- .NET 8 SDK；具体 SDK 约束以 `global.json` 为准。
- 框架依赖运行需要 .NET 8 Desktop Runtime；自包含发布不需要目标机安装运行时。

本项目依赖 Windows Named Pipes、ETW、Win32 API 和 Windows x64 原生库，不承诺 macOS/Linux 支持。

## 快速开始

在仓库根目录执行：

```powershell
dotnet restore ComputerCompanion.csproj
dotnet build ComputerCompanion.csproj -c Debug
dotnet test Tests\ComputerCompanion.Tests.csproj -c Debug
```

运行构建产物：

```powershell
.\bin\Debug\net8.0-windows\win-x64\ComputerCompanion.exe
```

悬浮窗由主进程按设置启动；手动调试悬浮窗时可使用：

```powershell
.\bin\Debug\net8.0-windows\win-x64\ComputerCompanion.exe --overlay
```

## 管理员权限

`app.manifest` 使用 `asInvoker`，程序不会强制以管理员身份启动。启动后会检查当前权限并提示用户；以管理员身份重新启动后，硬件传感器、ETW FPS 和部分 GPU 功能通常更完整。

## 文档

- [架构](docs/ARCHITECTURE.md)：进程模型、IPC、DI、数据流和模块边界。
- [部署](docs/DEPLOYMENT.md)：构建、发布、配置目录和部署检查。
- [维护](docs/MAINTENANCE.md)：日志、备份恢复、排障、测试和贡献。
- 各一级目录的 README：从 [Api/README.md](Api/README.md) 等目录说明开始阅读。

## 性能数据

仓库中的历史文档包含开发环境测量值，但不构成稳定性能承诺。使用 `scripts/benchmark.ps1` 在目标机器上生成实测结果，并记录硬件、权限、监控项和构建配置。

## 许可证

本项目采用 [MIT License](LICENSE)。
