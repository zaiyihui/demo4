using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ComputerCompanion.Models;
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
    private bool _isClickThroughEnabled = true;

    #endregion

    #region 构造函数

    public MainWindow()
    {
        InitializeComponent();
        
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        Topmost = true;
        ShowInTaskbar = true;

        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        
        Opened += OnWindowOpened;
        KeyDown += OnKeyDown;
        Deactivated += OnDeactivated;
    }

    #endregion

    #region 事件处理

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        SetClickThrough(_isClickThroughEnabled);
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // 窗口失去焦点时自动启用穿透
        if (_isClickThroughEnabled)
        {
            SetClickThrough(true);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _startPoint = e.GetPosition(this);
            SetClickThrough(false);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _startPoint;
            Position = new PixelPoint(Position.X + (int)offset.X, Position.Y + (int)offset.Y);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetClickThrough(_isClickThroughEnabled);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                break;
            case Key.F1:
                _isClickThroughEnabled = !_isClickThroughEnabled;
                SetClickThrough(_isClickThroughEnabled);
                UpdateStatusText();
                break;
            case Key.F2:
                ToggleGameMode();
                break;
            case Key.F3:
                OpenSettings();
                break;
        }
    }

    #endregion

    #region 设置窗口

    private void OpenSettings()
    {
        SetClickThrough(false);
        
        var settingsService = new SettingsService();
        var settings = settingsService.GetSettings();
        
        var settingsWindow = new SettingsWindow(settings, OnSettingsSaved);
        settingsWindow.ShowDialog(this);
    }

    private void OnSettingsSaved(Settings settings)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.UpdateSettings(settings);
        }

        if (settings.EnableOverlay)
        {
            App.RestartOverlayProcess();
        }
        else
        {
            App.StopOverlayProcess();
        }

        HandleAutoStart(settings.AutoStart);

        SetClickThrough(_isClickThroughEnabled);
    }
    
    private void HandleAutoStart(bool enable)
    {
        if (!OperatingSystem.IsWindows()) return;
        
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;
            
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            
            if (key == null) return;
            
            const string appName = "电脑伴侣";
            
            if (enable)
            {
                key.SetValue(appName, exePath);
            }
            else
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName);
                }
            }
            
            key.Close();
        }
        catch
        {
        }
    }

    #endregion

    #region 功能方法

    private void ToggleGameMode()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleGameMode();
            UpdateStatusText();
        }
    }

    private void UpdateStatusText()
    {
        // 状态更新由 ViewModel 处理
    }

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

    private void OnMinimizeToTrayClick(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    #endregion
}