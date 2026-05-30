# ViewModels 文件夹说明文档

## 1. 文件夹功能概述
**ViewModels** 文件夹用于存放项目的视图模型类。这些类是 MVVM 架构中的 VM（ViewModel）层，负责连接视图（View）和模型（Model），处理 UI 逻辑和数据绑定。

## 2. 包含内容

| 文件/文件夹 | 文件类型 | 说明 | 重要性 |
|------------|---------|------|--------|
| **MainWindowViewModel.cs** | C# 源代码 | 主窗口的视图模型，管理主界面的数据和逻辑 | ⭐⭐⭐⭐⭐ |
| **OverlayViewModel.cs** | C# 源代码 | 悬浮窗的视图模型，管理悬浮窗的数据和逻辑 | ⭐⭐⭐⭐⭐ |
| **SettingsViewModel.cs** | C# 源代码 | 设置窗口的视图模型，管理配置界面的数据和逻辑 | ⭐⭐⭐⭐⭐ |

## 3. 重要文件说明

### 3.1 MainWindowViewModel.cs
- **位置**：[ViewModels/MainWindowViewModel.cs](file:///d:/BC/xiangmu/demo4/demo4/ViewModels/MainWindowViewModel.cs)
- **功能**：主窗口的视图模型

#### 主要功能：
- 硬件监控数据的封装和暴露
- 主窗口显示逻辑
- 用户界面状态管理
- 与硬件监控服务的交互

#### 使用的框架：
- CommunityToolkit.Mvvm
- `[ObservableObject]` 和 `[ObservableProperty]` 属性

### 3.2 OverlayViewModel.cs
- **位置**：[ViewModels/OverlayViewModel.cs](file:///d:/BC/xiangmu/demo4/demo4/ViewModels/OverlayViewModel.cs)
- **功能**：悬浮窗的视图模型

#### 主要功能：
- 悬浮窗显示内容管理
- 位置和外观数据绑定
- 性能数据实时更新
- 与 IPC 服务的通信

### 3.3 SettingsViewModel.cs
- **位置**：[ViewModels/SettingsViewModel.cs](file:///d:/BC/xiangmu/demo4/demo4/ViewModels/SettingsViewModel.cs)
- **功能**：设置窗口的视图模型

#### 主要功能：
- 所有配置项的可观察属性
- 配置加载和保存命令
- 恢复默认设置命令
- UI 状态管理

#### 核心属性：
- `EnableOverlay`：悬浮窗启用状态
- `OverlayPosition`：悬浮窗位置
- `BackgroundOpacity`：背景透明度
- `OverlayFontSize`：字体大小
- `OverlayShowCpu`、`OverlayShowGpu` 等：显示内容开关

## 4. 使用规范

### 4.1 ViewModel 设计原则
- 使用 CommunityToolkit.Mvvm 的特性
- 继承 `ObservableObject`（通过 `[ObservableObject]`）
- 使用 `[ObservableProperty]` 标记可观察属性
- 使用 `[RelayCommand]` 标记命令方法

### 4.2 属性定义示例
```csharp
[ObservableProperty]
private string _title = "电脑伴侣";

[ObservableProperty]
private bool _enableOverlay = true;
```

### 4.3 命令定义示例
```csharp
[RelayCommand]
private void Save()
{
    // 保存逻辑
}

[RelayCommand]
private async Task SaveAsync()
{
    // 异步保存逻辑
}
```

### 4.4 数据绑定
在 XAML 中绑定到 ViewModel：
```xml
<TextBlock Text="{Binding Title}"/>
<CheckBox IsChecked="{Binding EnableOverlay}"/>
<Button Command="{Binding SaveCommand}" Content="保存"/>
```

## 5. 开发建议

### 5.1 添加新 ViewModel
1. 创建新的类文件
2. 添加 `[ObservableObject]` 属性
3. 定义可观察属性
4. 实现命令方法
5. 在 View 中绑定

### 5.2 最佳实践
- ViewModel 不应引用 UI 控件
- 保持 ViewModel 的可测试性
- 使用异步命令处理耗时操作
- 合理使用属性变更通知

### 5.3 服务访问
通过构造函数注入服务：
```csharp
public partial class MainWindowViewModel : ObservableObject
{
    private readonly HardwareMonitorService _hardwareMonitorService;
    
    public MainWindowViewModel(HardwareMonitorService hardwareMonitorService)
    {
        _hardwareMonitorService = hardwareMonitorService;
    }
}
```

## 6. 维护建议

- 保持 ViewModel 与 View 的分离
- 及时更新 ViewModel 以反映模型变更
- 添加单元测试覆盖 ViewModel 逻辑
- 避免 ViewModel 过于臃肿，考虑拆分
- 保持 ViewModel 说明文档的更新

## 7. 相关文档

- CommunityToolkit.Mvvm 文档：https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- MVVM 架构说明：[项目结构说明.md](../docs/项目结构说明.md)
- 设置窗口功能：[项目完整汇报总结.md](../docs/项目完整汇报总结.md)

---

**文档版本**：v1.0  
**创建日期**：2026-05-30  
**维护责任人**：项目维护团队
