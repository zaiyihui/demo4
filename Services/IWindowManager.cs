using Avalonia.Controls;
using ComputerCompanion.Views;
using System;

namespace ComputerCompanion.Services;

/// <summary>
/// 窗口管理器接口
/// 负责主窗口的显示、隐藏、位置管理和退出处理
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// 主窗口实例
    /// </summary>
    MainWindow? MainWindow { get; }
    
    /// <summary>
    /// 主窗口是否可见
    /// </summary>
    bool IsMainWindowVisible { get; }
    
    /// <summary>
    /// 设置主窗口
    /// </summary>
    void SetMainWindow(MainWindow window);
    
    /// <summary>
    /// 显示主窗口
    /// </summary>
    void ShowMainWindow();
    
    /// <summary>
    /// 隐藏主窗口
    /// </summary>
    void HideMainWindow();
    
    /// <summary>
    /// 切换主窗口可见性
    /// </summary>
    void ToggleMainWindow();
    
    /// <summary>
    /// 配置关闭按钮行为（隐藏而非退出）
    /// </summary>
    void ConfigureCloseToHideBehavior();
    
    /// <summary>
    /// 应用退出事件
    /// </summary>
    event EventHandler? ExitRequested;
}