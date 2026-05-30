# Models 文件夹说明文档

## 1. 文件夹功能概述
**Models** 文件夹用于存放项目的数据模型类和枚举定义。这些模型代表应用程序的数据结构，是 MVVM 架构中的 M（Model）层。

## 2. 包含内容

| 文件/文件夹 | 文件类型 | 说明 | 重要性 |
|------------|---------|------|--------|
| **Settings.cs** | C# 源代码 | 设置模型和枚举定义 | ⭐⭐⭐⭐⭐ |

## 3. 重要文件说明

### Settings.cs
- **位置**：[Models/Settings.cs](file:///d:/BC/xiangmu/demo4/demo4/Models/Settings.cs)
- **功能**：定义应用程序的所有配置项和相关枚举

#### 主要内容：

##### 3.1 Settings 类
应用程序的核心配置模型，包含以下配置分类：

| 配置分类 | 属性列表 |
|---------|---------|
| **界面外观** | `LayoutMode`、`TextColor`、`BackgroundColor`、`BackgroundOpacity`、`FontSize` |
| **性能设置** | `RefreshInterval`、`GameMode`、`GameModeRefreshInterval` |
| **显示内容** | `ShowCpu`、`ShowGpu`、`ShowMemory`、`ShowNetwork`、`ShowDisk`、`ShowBattery` |
| **窗口位置** | `WindowX`、`WindowY` |
| **悬浮窗设置** | `EnableOverlay`、`OverlayAlwaysOnTop`、`OverlayFontSize`、`OverlayTextColor`、`OverlayPosition`、`OverlayShowFPS`、`OverlayShowGpu`、`OverlayShowCpu`、`OverlayShowMemory`、`OverlayShowLatency` |
| **启动设置** | `AutoStart`、`StartMinimized` |

##### 3.2 枚举定义

**LayoutMode 枚举**
- `Vertical`：垂直布局模式
- `Horizontal`：水平布局模式

**OverlayPosition 枚举**
- `TopLeft`：左上角
- `TopRight`：右上角
- `BottomLeft`：左下角
- `BottomRight`：右下角

## 4. 使用规范

### 4.1 模型设计原则
- 模型类应该是 POCO（Plain Old CLR Object）
- 包含默认值，确保配置有合理的初始状态
- 属性使用明确的类型，避免 `object` 或 `dynamic`

### 4.2 在代码中使用
```csharp
// 创建新的设置实例
var settings = new Settings();

// 修改配置
settings.EnableOverlay = true;
settings.OverlayPosition = OverlayPosition.TopRight;

// 保存配置
_settingsService.SaveSettings(settings);
```

### 4.3 序列化注意事项
- Settings 类使用 Newtonsoft.Json 进行序列化
- 确保所有属性类型都是可序列化的
- 复杂类型需要自定义 JSON 转换器

## 5. 扩展建议

### 5.1 添加新配置项
1. 在 Settings 类中添加新属性
2. 设置合理的默认值
3. 在 SettingsViewModel 中添加对应的可观察属性
4. 在 SettingsWindow.axaml 中添加 UI 控件
5. 更新 LoadSettings 和 Save 方法

### 5.2 版本兼容性
- 如需对配置模型进行重大更改，考虑添加版本号
- 提供配置迁移逻辑
- 保持向后兼容性

## 6. 维护建议

- 添加新配置时，确保在所有相关位置同步更新
- 保持默认值的合理性
- 考虑配置的持久化和迁移策略
- 定期检查是否有冗余配置项
- 添加新模型类时，及时更新本文档

## 7. 相关文档

- 设置服务详细说明请参考：[设置服务.md](../docs/模块文档/设置服务.md)
- 完整的项目结构请参考：[项目结构说明.md](../docs/项目结构说明.md)

---

**文档版本**：v1.0  
**创建日期**：2026-05-30  
**维护责任人**：项目维护团队
