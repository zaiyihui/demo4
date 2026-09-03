# Models

## 职责
定义设置、监控数据、图表、颜色和皮肤等领域模型，不负责窗口生命周期或外部 I/O。

## 关键文件

| 文件 | 职责 |
|---|---|
| `Settings.cs` | 聚合所有设置子模块 |
| `MainWindowSettings.cs` | 主窗口布局设置 |
| `OverlaySettings.cs` | 悬浮窗显示设置 |
| `DisplayContentSettings.cs` | 主窗口指标显示开关 |
| `PerformanceSettings.cs` | 刷新、主题和性能设置 |
| `StartupSettings.cs` | 启动行为设置 |
| `DataStorageSettings.cs` | 数据目录设置 |
| `ChartModels.cs` | 图表和导出数据模型 |
| `SkinPreset.cs` / `ColorPresets.cs` | 皮肤和颜色预设 |

## 注意事项
修改 JSON 属性时考虑已有配置兼容性；顶层旧属性仅用于向后兼容，新增代码应使用设置子模块。