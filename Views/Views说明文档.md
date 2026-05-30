# Views 文件夹说明文档

## 1. 文件夹功能概述
**Views** 文件夹用于存放项目的 UI 视图文件（.axaml）和对应的代码隐藏文件（.axaml.cs）。这些文件构成了应用程序的用户界面，是 MVVM 架构中的 V（View）层。

## 2. 包含内容

| 文件/文件夹 | 文件类型 | 说明 | 重要性 |
|------------|---------|------|--------|
| **MainWindow.axaml** | Avalonia XAML | 主窗口的 UI 定义 | ⭐⭐⭐⭐⭐ |
| **MainWindow.axaml.cs** | C# 源代码 | 主窗口的代码隐藏文件 | ⭐⭐⭐⭐⭐ |
| **OverlayWindow.axaml** | Avalonia XAML | 悬浮窗的 UI 定义 | ⭐⭐⭐⭐⭐ |
| **OverlayWindow.axaml.cs** | C# 源代码 | 悬浮窗的代码隐藏文件 | ⭐⭐⭐⭐⭐ |
| **SettingsWindow.axaml** | Avalonia XAML | 设置窗口的 UI 定义（NVIDIA 风格） | ⭐⭐⭐⭐⭐ |
| **SettingsWindow.axaml.cs** | C# 源代码 | 设置窗口的代码隐藏文件 | ⭐⭐⭐⭐⭐ |

## 3. 重要文件说明

### 3.1 MainWindow.axaml
- **位置**：[Views/MainWindow.axaml](file:///d:/BC/xiangmu/demo4/demo4/Views/MainWindow.axaml)
- **功能**：应用程序的主界面，显示所有硬件监控数据

#### 主要界面元素：
- 硬件数据展示区域（CPU、GPU、内存、网络等）
- 系统状态指示
- 导航和操作按钮
- 配置入口

#### 窗口特性：
- 半透明背景
- 可调整大小
- 可拖动位置
- 支持最小化到托盘

### 3.2 MainWindow.axaml.cs
- **位置**：[Views/MainWindow.axaml.cs](file:///d:/BC/xiangmu/demo4/demo4/Views/MainWindow.axaml.cs)
- **功能**：主窗口的事件处理和逻辑代码

#### 主要功能：
- 窗口初始化
- 事件处理程序
- 与 ViewModel 的交互
- 窗口状态管理

### 3.3 OverlayWindow.axaml
- **位置**：[Views/OverlayWindow.axaml](file:///d:/BC/xiangmu/demo4/demo4/Views/OverlayWindow.axaml)
- **功能**：游戏悬浮窗，显示关键性能指标

#### 主要界面元素：
- FPS 显示
- CPU/GPU 使用率
- 内存使用
- 网络延迟
- 可配置的显示内容

#### 窗口特性：
- 总是置顶
- 透明背景
- 无边框设计
- 可拖动
- 支持四个角落位置

### 3.4 OverlayWindow.axaml.cs
- **位置**：[Views/OverlayWindow.axaml.cs](file:///d:/BC/xiangmu/demo4/demo4/Views/OverlayWindow.axaml.cs)
- **功能**：悬浮窗的事件处理和逻辑代码

#### 主要功能：
- 悬浮窗位置管理
- 显示内容更新
- IPC 消息处理
- 窗口状态控制

### 3.5 SettingsWindow.axaml
- **位置**：[Views/SettingsWindow.axaml](file:///d:/BC/xiangmu/demo4/demo4/Views/SettingsWindow.axaml)
- **功能**：NVIDIA 风格的设置窗口，提供所有配置选项

#### 主要面板：
1. **概览面板**：显示当前配置状态
2. **显示设置**：悬浮窗开关、位置、置顶等
3. **外观**：透明度、字体、颜色
4. **显示内容**：选择要显示的性能指标
5. **性能**：刷新频率、游戏模式
6. **启动选项**：自动启动、启动最小化

#### 界面特性：
- 左侧导航栏
- 右侧内容区域
- 应用/取消/恢复默认按钮
- 实时预览区域

### 3.6 SettingsWindow.axaml.cs
- **位置**：[Views/SettingsWindow.axaml.cs](file:///d:/BC/xiangmu/demo4/demo4/Views/SettingsWindow.axaml.cs)
- **功能**：设置窗口的事件处理和逻辑代码

#### 主要功能：
- 导航面板切换
- 按钮事件处理
- 颜色预设管理
- 与 ViewModel 的交互

## 4. 使用规范

### 4.1 XAML 文件结构
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="ComputerCompanion.Views.MainWindow"
        x:DataType="viewModels:MainWindowViewModel">
    
    <!-- UI 内容 -->
    
</Window>
```

### 4.2 数据绑定
```xml
<!-- 绑定到 ViewModel 属性 -->
<TextBlock Text="{Binding Title}"/>
<CheckBox IsChecked="{Binding EnableOverlay}"/>

<!-- 绑定到命令 -->
<Button Command="{Binding SaveCommand}"/>
```

### 4.3 代码隐藏文件
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // 事件处理逻辑
    }
}
```

## 5. 开发建议

### 5.1 View 设计原则
- 保持 View 简洁，只包含 UI 逻辑
- 使用数据绑定而非直接操作控件
- 充分利用样式和资源
- 考虑响应式布局

### 5.2 添加新窗口
1. 创建 .axaml 文件
2. 创建对应的 .axaml.cs 文件
3. 定义 UI 结构
4. 添加事件处理程序
5. 在 App.axaml.cs 中注册（如需要）

### 5.3 最佳实践
- 使用 x:DataType 指定数据上下文类型
- 合理使用用户控件（UserControl）
- 保持 XAML 的可读性
- 注释复杂的布局逻辑

## 6. 维护建议

- 保持 View 与 ViewModel 的分离
- 定期检查未使用的 UI 元素
- 确保所有 UI 字符串使用本地化资源
- 测试不同分辨率下的显示效果
- 保持视图说明文档的更新

## 7. 相关文档

- Avalonia UI 文档：https://docs.avaloniaui.net/
- 设置窗口功能：[项目完整汇报总结.md](../docs/项目完整汇报总结.md)
- 项目结构说明：[项目结构说明.md](../docs/项目结构说明.md)

---

**文档版本**：v1.0  
**创建日期**：2026-05-30  
**维护责任人**：项目维护团队
