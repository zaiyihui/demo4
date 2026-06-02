# 电脑伴侣 - 部署安装指南

## 目录

1. [环境要求](#环境要求)
2. [依赖安装](#依赖安装)
3. [编译流程](#编译流程)
4. [配置说明](#配置说明)
5. [启动方法](#启动方法)
6. [常见问题排查](#常见问题排查)

---

## 1. 环境要求

### 1.1 操作系统
- **Windows 10** (版本 1809 或更高)
- **Windows 11** (推荐)

### 1.2 .NET Framework
- **.NET 8.0 SDK** 或更高版本

### 1.3 硬件要求
- CPU: Intel i5-6500 或同等性能处理器
- 内存: 8GB RAM 或更高
- 显卡: 支持 DirectX 11 的图形卡（用于悬浮窗透明效果）

### 1.4 软件依赖
- **LibreHardwareMonitor** - 硬件监控库（项目已包含 NuGet 包）

---

## 2. 依赖安装

### 2.1 安装 .NET 8.0 SDK

1. 访问 [.NET 官方下载页面](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 下载并安装适用于 Windows 的 .NET 8.0 SDK
3. 验证安装：
   ```bash
   dotnet --version
   ```
   输出应显示 `8.0.x` 版本号

### 2.2 克隆项目

```bash
git clone <repository-url>
cd ComputerCompanion
```

### 2.3 恢复 NuGet 依赖

```bash
dotnet restore
```

---

## 3. 编译流程

### 3.1 开发环境编译

```bash
dotnet build --configuration Debug
```

### 3.2 生产环境编译

```bash
dotnet build --configuration Release
```

### 3.3 发布独立可执行文件

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true /p:PublishSingleFile=true
```

输出目录：`bin/Release/net8.0-windows/win-x64/publish/`

---

## 4. 配置说明

### 4.1 配置文件位置

配置文件存储在用户目录下：
```
%APPDATA%\ComputerCompanion\settings.json
```

### 4.2 配置项说明

| 配置项 | 类型 | 默认值 | 说明 |
|-------|------|-------|------|
| `LayoutMode` | string | Vertical | 主窗口布局模式（Vertical/Horizontal） |
| `TextColor` | string | #FFFFFF | 文字颜色 |
| `BackgroundColor` | string | #1a1a2eea | 背景颜色 |
| `BackgroundOpacity` | double | 0.9 | 背景透明度（0-1） |
| `FontSize` | int | 14 | 字体大小 |
| `RefreshInterval` | int | 1000 | 数据刷新间隔（毫秒） |
| `GameMode` | bool | false | 是否启用游戏模式 |
| `GameModeRefreshInterval` | int | 3000 | 游戏模式刷新间隔 |
| `ShowCpu` | bool | true | 显示 CPU 信息 |
| `ShowGpu` | bool | true | 显示 GPU 信息 |
| `ShowMemory` | bool | true | 显示内存信息 |
| `ShowNetwork` | bool | true | 显示网络信息 |
| `ShowDisk` | bool | true | 显示磁盘信息 |
| `ShowBattery` | bool | true | 显示电池信息 |
| `EnableOverlay` | bool | true | 启用悬浮窗 |
| `OverlayAlwaysOnTop` | bool | true | 悬浮窗置顶 |
| `OverlayFontSize` | int | 16 | 悬浮窗字体大小 |
| `OverlayTextColor` | string | #76B900 | 悬浮窗文字颜色 |
| `OverlayPosition` | string | TopRight | 悬浮窗位置 |
| `AutoStart` | bool | false | 开机自动启动 |
| `StartMinimized` | bool | false | 启动时最小化 |

---

## 5. 启动方法

### 5.1 开发模式运行

```bash
dotnet run
```

### 5.2 运行发布的可执行文件

```bash
./bin/Release/net8.0-windows/win-x64/publish/ComputerCompanion.exe
```

### 5.3 启动参数

| 参数 | 说明 |
|------|------|
| `--overlay` | 以悬浮窗模式启动 |

示例：
```bash
ComputerCompanion.exe --overlay
```

---

## 6. 常见问题排查

### 6.1 编译错误

**问题**: `error CS0246: 找不到类型或命名空间名称`

**解决方案**:
1. 确保已运行 `dotnet restore`
2. 检查项目引用是否完整
3. 清理并重新构建：
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

### 6.2 硬件监控不工作

**问题**: CPU/GPU 信息显示为 `--`

**解决方案**:
1. 确保以管理员身份运行程序
2. 检查是否安装了正确的显卡驱动
3. 确认 LibreHardwareMonitor 支持当前硬件

### 6.3 悬浮窗不显示

**问题**: 悬浮窗启用但不显示

**解决方案**:
1. 检查是否有多个显示器
2. 尝试切换悬浮窗位置设置
3. 检查是否被其他窗口遮挡
4. 确保显卡支持透明效果（Windows Aero）

### 6.4 开机自启不生效

**问题**: 设置了开机自启但程序未启动

**解决方案**:
1. 检查是否有足够权限修改注册表
2. 尝试以管理员身份运行一次程序
3. 手动检查注册表项：
   ```
   HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
   ```
   确认存在 `电脑伴侣` 键值，指向正确的可执行文件路径

### 6.5 权限问题

**问题**: 提示"权限不足"错误

**解决方案**:
1. 右键点击程序，选择"以管理员身份运行"
2. 或在属性中设置"兼容性"选项为"以管理员身份运行此程序"

---

## 附录：日志位置

程序运行日志输出到控制台。如需持久化日志，可重定向输出：

```bash
ComputerCompanion.exe > log.txt 2>&1
```

---

**文档版本**: v1.0  
**最后更新**: 2026年6月