# ViewModels

## 职责
连接 Avalonia 视图与服务层，管理界面状态、命令、定时刷新和用户交互。

## 关键文件

| 文件 | 职责 |
|---|---|
| `MainWindowViewModel.cs` | 主监控窗口状态 |
| `OverlayViewModel.cs` | 悬浮窗指标和显示模式 |
| `SettingsViewModel.cs` | 设置编辑和保存 |
| `PerformanceDashboardViewModel.cs` | 性能图表、历史数据和告警展示 |

## 注意事项
遵循 MVVM，避免在 ViewModel 中直接创建不可替换的硬件或文件依赖；UI 更新必须回到 Avalonia UI 线程。