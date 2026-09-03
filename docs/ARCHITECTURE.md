# ComputerCompanion 架构

## 定位

ComputerCompanion 是面向 Windows x64 的 .NET 8 + Avalonia 11.2.0 桌面硬件监控工具。主进程负责监控、设置、主窗口和托盘；悬浮窗作为同一程序的 `--overlay` 子进程运行。

## 进程模型

```text
主进程
  HardwareMonitorService / FpsMonitorService
  SettingsService / DataStorageService
  MainWindow / SettingsWindow / TrayIconService
  IpcService（Named Pipe server）
             <---- ComputerCompanion_IPC ---->
悬浮窗进程（--overlay）
  OverlayWindow / OverlayViewModel
  IpcService（Named Pipe client）
```

主进程通过 `OverlayProcessManager` 启动、停止和有限次数恢复悬浮窗。悬浮窗崩溃不会直接终止主进程；退出和设置同步通过 IPC 传递。

## 技术分层

- **Views/Styles/Converters**：Avalonia 窗口、控件样式和绑定转换。
- **ViewModels**：以 MVVM 方式组织主窗口、悬浮窗、设置和性能面板状态。
- **Services**：硬件采集、FPS、网络、延迟、电池、设置、导出、告警、主题、托盘、IPC 等运行时服务。
- **Models**：设置子模块、图表点、皮肤和颜色预设等数据结构。
- **Core**：日志、备份、本地规则洞察、插件和性能抽象。
- **Plugins/Api**：插件元数据/加载入口及 API 数据传输对象。

服务在 `App.axaml.cs` 的 `ConfigureServices` 中通过 `Microsoft.Extensions.DependencyInjection` 注册。视图模型通过接口依赖服务，便于测试替换硬件和文件系统依赖。

## 数据流

1. `HardwareMonitorService` 使用 LibreHardwareMonitorLib 读取 CPU、GPU、内存和传感器；网络、延迟、电池由拆分服务采集。
2. `FpsMonitorService` 使用 Windows ETW 追踪 DXGI Present 事件，计算 FPS、Frame Time 和 1% Low；没有管理员权限时该能力可能不可用。
3. 服务通过事件或属性更新 ViewModel，Avalonia 绑定刷新界面。
4. `DataStorageService` 解析数据目录；设置、日志、缓存和导出数据写入该目录。
5. 主进程将悬浮窗设置、就绪和退出消息发送给子进程。

## IPC

- 管道名：`ComputerCompanion_IPC`。
- 传输：Windows Named Pipes，双向字节流，UTF-8 JSON。
- 帧：4 字节 little-endian 长度前缀加 JSON 内容，消息上限 64 KiB。
- 消息路由：`IpcMessageRouter`；当前消息类型包括 `SessionKey`、`SettingsChanged`、`ShowMainWindow`、`ExitApplication` 和 `OverlayReady`。
- 安全：可注入 `ISecurityService` 生成会话密钥并验证消息；解析失败、超长消息和断线由 `IpcService` 处理，并按客户端循环重连。

## 关键设计决策

- Windows-only：目标框架为 `net8.0-windows`，运行时标识为 `win-x64`，不承诺 macOS/Linux 支持。
- `app.manifest` 使用 `asInvoker`，启动后检测管理员状态并引导用户以管理员身份重启，而不是强制系统弹窗。
- 监控启动放入后台任务，优先显示窗口，降低启动阶段对 UI 的阻塞。
- 皮肤由 `Assets/Skins/*.json` 和 `SkinService` 管理；内置 minimal、tech、retro、night 预设。
- `InsightService` 仅提供本地规则洞察，不依赖外部服务。

## 相关入口

- [部署说明](DEPLOYMENT.md)
- [维护说明](MAINTENANCE.md)
- [目录说明](../README.md)
