# Styles

## 职责
集中管理 Avalonia 全局资源、窗口样式、导航样式和设置页面样式。

## 关键文件

| 文件 | 职责 |
|---|---|
| `GlobalResources.axaml` | 全局颜色、字体和资源 |
| `GlobalStyles.axaml` | 通用控件样式 |
| `NavigationStyles.axaml` | 导航相关样式 |
| `SettingsStyles.axaml` | 设置窗口样式 |

## 注意事项
优先复用资源键和已有选择器，避免在视图中重复硬编码主题颜色；修改样式后检查主窗口、悬浮窗和设置窗口。