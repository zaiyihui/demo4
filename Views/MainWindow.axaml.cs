using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using System;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Views;

/// <summary>
/// 主窗口类 - 悬浮窗形式的硬件监控界面
/// 支持鼠标穿透、窗口拖拽、快捷键操作
/// </summary>
public partial class MainWindow : Window
{
    #region Win32 API 声明

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    #endregion

    #region 私有字段

    /// <summary>
    /// 鼠标拖拽起始点
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// 是否启用鼠标穿透模式
    /// </summary>
    private bool _isClickThroughEnabled = true; // 默认开启穿透

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化主窗口
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // 配置无边框窗口样式
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        Topmost = true;
        ShowInTaskbar = true;

        // 注册鼠标事件处理器（使用隧道模式确保优先处理）
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        
        // 注册窗口事件
        Opened += OnWindowOpened;
        KeyDown += OnKeyDown;
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 窗口打开事件
    /// </summary>
    private void OnWindowOpened(object? sender, EventArgs e)
    {
        SetClickThrough(_isClickThroughEnabled);
    }

    /// <summary>
    /// 鼠标按下事件处理
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 记录拖拽起始点
            _startPoint = e.GetPosition(this);
            // 拖拽时暂时关闭穿透，允许窗口被拖动
            SetClickThrough(false);
        }
    }

    /// <summary>
    /// 鼠标移动事件处理
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 计算鼠标偏移并移动窗口
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _startPoint;
            Position = new PixelPoint(Position.X + (int)offset.X, Position.Y + (int)offset.Y);
        }
    }

    /// <summary>
    /// 鼠标释放事件处理
    /// </summary>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 释放鼠标后重新启用穿透
        SetClickThrough(true);
    }

    /// <summary>
    /// 键盘按键事件处理
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                // 关闭窗口
                Close();
                break;
            case Key.F1:
                // 切换鼠标穿透模式
                _isClickThroughEnabled = !_isClickThroughEnabled;
                SetClickThrough(_isClickThroughEnabled);
                break;
            case Key.F2:
                // 切换游戏模式
                ToggleGameMode();
                break;
            case Key.F3:
                // 打开设置窗口
                OpenSettings();
                break;
        }
    }

    #endregion

    #region 设置窗口

    /// <summary>
    /// 打开设置窗口
    /// </summary>
    private void OpenSettings()
    {
        SetClickThrough(false);
        
        var settingsService = new SettingsService();
        var settings = settingsService.GetSettings();
        
        var settingsWindow = new SettingsWindow(settings, OnSettingsSaved);
        settingsWindow.ShowDialog(this);
    }

    /// <summary>
    /// 设置保存回调
    /// </summary>
    private void OnSettingsSaved(Settings settings)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.UpdateSettings(settings);
        }
        SetClickThrough(true);
    }

    #endregion

    #region 功能方法

    /// <summary>
    /// 切换游戏模式（降低刷新率以减少性能消耗）
    /// </summary>
    private void ToggleGameMode()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleGameMode();
        }
    }

    /// <summary>
    /// 设置鼠标穿透状态
    /// </summary>
    /// <param name="enable">是否启用穿透</param>
    private void SetClickThrough(bool enable)
    {
        if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
        {
            int style = GetWindowLong(handle.Handle, GWL_EXSTYLE);
            
            if (enable)
            {
                style |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            }
            else
            {
                style &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLong(handle.Handle, GWL_EXSTYLE, style);
        }
    }

    #endregion
}
