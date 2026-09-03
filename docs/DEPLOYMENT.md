# ComputerCompanion 部署

## 支持范围与要求

- Windows 10/11 x64；项目不支持 macOS/Linux。
- .NET SDK 8.0；仓库中的 `global.json` 用于约束 SDK 版本。
- 开发构建默认是框架依赖模式，目标机需要 .NET 8 Desktop Runtime。
- 发布时可显式指定 `win-x64` 和 `--self-contained true`，目标机无需安装 .NET Runtime。
- 完整硬件传感器、ETW 真实 FPS 和部分 GPU 能力建议管理员运行。

## 获取依赖与构建

在仓库根目录执行：

```powershell
dotnet restore ComputerCompanion.csproj
dotnet build ComputerCompanion.csproj -c Debug
```

本项目包含 Avalonia 原生依赖、SkiaSharp 和 LibreHardwareMonitor。内存紧张时使用单进程构建：

```powershell
dotnet build ComputerCompanion.csproj -c Release /m:1 /nodeReuse:false /p:UseSharedCompilation=false
```

也可以使用根目录 `构建脚本.ps1` 或 `scripts/` 下的维护脚本。脚本执行策略受限时，在当前用户范围调整为 `RemoteSigned`，或按组织策略使用受控的执行方式。

## 发布

框架依赖发布：

```powershell
dotnet publish ComputerCompanion.csproj -c Release -r win-x64 --self-contained false -o bin\Release\publish
```

自包含发布：

```powershell
dotnet publish ComputerCompanion.csproj -c Release -r win-x64 --self-contained true -o bin\Release\publish
```

发布目录应整体复制，不能只复制主 EXE：Avalonia ANGLE 原生 DLL、LibreHardwareMonitorLib、配置资源和皮肤文件都可能是运行时依赖。发布后优先从 `bin\Release\publish\ComputerCompanion.exe` 启动。

## 运行方式

```powershell
.\bin\Debug\net8.0-windows\win-x64\ComputerCompanion.exe
.\bin\Debug\net8.0-windows\win-x64\ComputerCompanion.exe --overlay
```

不应单独启动 `--overlay`，悬浮窗需要主进程提供 Named Pipe 服务。正常启动主进程后，应用根据设置管理悬浮窗子进程。

## 权限引导

清单文件 `app.manifest` 配置为 `asInvoker`，因此程序先以当前用户权限启动。`Program` 检测当前令牌；非管理员运行时会记录提示，ETW FPS 或部分 LibreHardwareMonitor 传感器可能不可用。用户可从程序提供的入口确认后，以管理员身份重新启动。

## 数据与配置

`DataStorageService` 根据 `Settings.DataStorage` 选择数据目录，支持：

- `%APPDATA%\ComputerCompanion`（默认）
- 安装目录
- 用户指定的自定义目录

目录通常包含 `settings.json`、`settings.rules.json`、`logs/`、`cache/` 及导出文件。设置损坏时服务会尝试生成 `.bak` 并回退默认设置。皮肤 JSON 位于 `Assets/Skins/`，当前内置 `minimal.json`、`tech.json`、`retro.json`、`night.json`。

## 部署检查

1. 确认 Windows x64 和目标运行时模式符合预期。
2. 确认发布目录包含所有原生 DLL 和皮肤资源。
3. 普通用户启动一次，确认窗口和配置目录可创建。
4. 以管理员启动一次，确认硬件数据、FPS 和悬浮窗 IPC 正常。
5. 检查日志目录中的启动、权限和服务初始化记录。
