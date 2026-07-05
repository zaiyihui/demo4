# 电脑伴侣 - 项目文档

## 项目概述

「电脑伴侣」是一款高性能硬件监控和游戏性能显示工具，基于 Avalonia UI 框架开发，支持实时监控 CPU、GPU、内存、网络等系统状态。

## 文档目录

### 入门指南
- [安装指南](INSTALLATION.md) - 如何安装和运行项目
- [快速开始](QUICK_START.md) - 快速上手使用指南

### 技术文档
- [项目结构说明](项目结构说明.md) - 项目目录结构和代码组织
- [IPC 协议规范](IPC_PROTOCOL.md) - 进程间通信协议说明

### 模块文档
- [硬件监控服务](模块文档/硬件监控服务.md) - LibreHardwareMonitor 集成说明
- [设置服务](模块文档/设置服务.md) - 配置管理服务说明
- [进程间通信服务](模块文档/进程间通信服务.md) - IPC 服务实现说明

### 运维文档
- [部署运维指南](部署运维指南.md) - 部署和运维相关文档
- [维护指南](维护指南.md) - 项目维护和升级指南

### 本地化
- [中文本地化支持指南](中文本地化支持指南.md) - 多语言支持说明

## 项目结构

```
ComputerCompanion/
├── src/                    # 源代码目录
│   └── ComputerCompanion.Overlay/  # 悬浮窗子项目
├── Assets/                 # 资源文件
├── Converters/             # 值转换器
├── Localization/           # 本地化资源
├── Models/                 # 数据模型
├── Services/               # 服务层
├── Styles/                 # 样式文件
├── Tests/                  # 测试项目
├── ViewModels/             # 视图模型
├── Views/                  # 视图
├── docs/                   # 文档目录
└── ComputerCompanion.csproj
```

## 核心功能

1. **硬件监控** - 实时监控 CPU、GPU、内存、磁盘、网络状态
2. **游戏悬浮窗** - 游戏中显示性能指标
3. **自动启动** - 支持开机自动启动
4. **颜色主题** - 支持多种预设颜色主题

## 技术栈

- **框架**: Avalonia UI 11.2.0
- **语言**: C# 8.0 (.NET 8)
- **MVVM**: CommunityToolkit.Mvvm
- **硬件监控**: LibreHardwareMonitorLib
- **JSON序列化**: Newtonsoft.Json

## 开发指南

### 开发环境要求
- .NET 8 SDK
- Visual Studio 2022 或 Rider
- Avalonia VS 扩展（可选）

### 构建命令

```powershell
# 构建
dotnet build ComputerCompanion.csproj -c Release

# 发布
dotnet publish ComputerCompanion.csproj -c Release -r win-x64 --self-contained true

# 运行测试
dotnet test Tests/ComputerCompanion.Tests.csproj
```

## 贡献指南

欢迎提交 Issue 和 Pull Request！

### 代码规范
- 遵循 .NET 编码规范
- 使用 `#nullable enable`
- 方法和变量命名使用 PascalCase/camelCase
- 提交信息遵循 Conventional Commits 规范

## 许可证

MIT License