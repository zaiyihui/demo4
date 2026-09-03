# Services

## 职责
实现应用运行时服务，包括硬件和 FPS 采集、网络/电池监控、设置、IPC、窗口、托盘、告警、主题和数据导出。

## 关键文件

| 文件 | 职责 |
|---|---|
| `HardwareMonitorService.cs` | LibreHardwareMonitor 硬件采集 |
| `FpsMonitorService.cs` | ETW/DXGI 真实 FPS、Frame Time 和 1% Low |
| `SettingsService.cs` / `DataStorageService.cs` | 设置持久化和数据目录 |
| `IpcService.cs` / `IpcMessageRouter.cs` | 主进程与悬浮窗 IPC |
| `OverlayProcessManager.cs` | 悬浮窗子进程生命周期和恢复 |
| `NetworkMonitorService.cs` / `LatencyMonitorService.cs` / `BatteryMonitorService.cs` | 网络、延迟和电池数据 |
| `AlertRuleService.cs` / `AlertSoundService.cs` | 规则告警和声音提示 |
| `SkinService.cs` / `ThemeService.cs` | 皮肤和主题 |
| `DataExportService.cs` | CSV、JSON、HTML 导出 |

## 注意事项
多数服务由 `App.ConfigureServices` 注册为单例；硬件、ETW、Named Pipe 和 Timer 资源必须正确释放，耗时工作不得阻塞 UI 线程。