# Styles 文件夹说明文档

## 1. 文件夹功能概述
**Styles** 文件夹用于存放 Avalonia UI 框架的样式文件（.axaml）。这些样式文件定义了应用程序的视觉外观和主题，确保 UI 元素在不同平台上的一致性。

## 2. 包含内容

| 文件/文件夹 | 文件类型 | 说明 | 重要性 |
|------------|---------|------|--------|
| **GlobalStyles.axaml** | Avalonia XAML | 全局样式文件，定义通用控件样式 | ⭐⭐⭐⭐⭐ |
| **SettingsStyles.axaml** | Avalonia XAML | 设置窗口专用样式文件 | ⭐⭐⭐⭐ |

## 3. 重要文件说明

### 3.1 GlobalStyles.axaml
- **位置**：[Styles/GlobalStyles.axaml](file:///d:/BC/xiangmu/demo4/demo4/Styles/GlobalStyles.axaml)
- **功能**：定义应用程序的全局样式，适用于所有窗口

#### 包含的样式：
- **TextBlock 样式**：基础文本样式、标题样式、副标题样式、正文字样、值显示样式、提示文字样式
- **Button 样式**：按钮通用样式
- **TextBox 样式**：文本框样式
- **CheckBox 样式**：复选框样式
- **RadioButton 样式**：单选按钮样式
- **ScrollViewer 样式**：滚动查看器样式

### 3.2 SettingsStyles.axaml
- **位置**：[Styles/SettingsStyles.axaml](file:///d:/BC/xiangmu/demo4/demo4/Styles/SettingsStyles.axaml)
- **功能**：设置窗口的专用样式，定义 NVIDIA 控制面板风格的界面

#### 包含的样式：
- 导航按钮样式（NavItem）
- 窗口控制按钮样式（Minimize、Maximize、Close）
- 操作按钮样式（Apply、Cancel、Reset）
- 位置选择按钮样式（PositionButton）
- 颜色预设按钮样式（ColorPreset）

## 4. 使用规范

### 4.1 样式文件结构
```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 样式定义 -->
    <Style Selector="Button">
        <Setter Property="FontSize" Value="14"/>
    </Style>
    
</Styles>
```

### 4.2 样式选择器
使用 CSS 风格的选择器：
- `Button`：所有按钮
- `Button.primary`：类名为 primary 的按钮
- `Button#submit`：ID 为 submit 的按钮
- `StackPanel > Button`：StackPanel 的直接子按钮

### 4.3 样式引用
在 App.axaml 中引用样式文件：
```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://电脑伴侣/Styles/GlobalStyles.axaml"/>
    <StyleInclude Source="avares://电脑伴侣/Styles/SettingsStyles.axaml"/>
</Application.Styles>
```

## 5. 开发建议

### 5.1 样式组织
- 按功能或控件类型组织样式
- 使用有意义的样式类名
- 考虑使用主题资源字典

### 5.2 样式优先级
- 内联样式优先级最高
- 其次是控件样式
- 最后是全局样式

### 5.3 最佳实践
- 避免在 XAML 中硬编码颜色值
- 使用资源引用（`{DynamicResource}`）
- 保持样式文件的简洁和可维护

## 6. 维护建议

- 定期检查未使用的样式，及时清理
- 保持样式文件的注释和文档
- 添加新样式时，考虑跨平台兼容性
- 测试不同主题和颜色模式下的显示效果
- 保持样式与设计规范一致

## 7. 相关文档

- Avalonia UI 样式文档：https://docs.avaloniaui.net/docs/styles/
- 项目配置和部署：[部署运维指南.md](../docs/部署运维指南.md)

---

**文档版本**：v1.0  
**创建日期**：2026-05-30  
**维护责任人**：项目维护团队
