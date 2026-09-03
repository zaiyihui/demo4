# Views

## 职责
定义 Avalonia 窗口及其代码后端，负责布局、绑定和窗口级交互。

## 关键文件

| 文件 | 职责 |
|---|---|
| `MainWindow.axaml(.cs)` | 主监控窗口 |
| `OverlayWindow.axaml(.cs)` | 透明置顶悬浮窗 |
| `SettingsWindow.axaml(.cs)` | 设置窗口 |
| `PerformanceDashboardWindow.axaml(.cs)` | 性能面板窗口 |

## 注意事项
业务逻辑放在 ViewModel 或 Services；悬浮窗运行在 `--overlay` 子进程，窗口关闭和 IPC 生命周期需保持一致。