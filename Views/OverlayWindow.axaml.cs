using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using ComputerCompanion;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using System;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Views;

public partial class OverlayWindow : Window
{
    private OverlayViewModel? _viewModel;
    private DispatcherTimer? _frameTimer;
    private Point _dragStartPoint;
    private bool _isDragging = false;
    private bool _isInitialized = false;

    public OverlayWindow()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
    }

    public void Initialize(OverlayViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        SetWindowTransparent();

        _frameTimer = new DispatcherTimer();
        _frameTimer.Interval = TimeSpan.FromMilliseconds(16);
        _frameTimer.Tick += OnFrameTick;
        _frameTimer.Start();

        RegisterIpcHandlers();

        // 等待窗口完全加载后再设置位置
        this.Loaded += (s, e) =>
        {
            _isInitialized = true;
            PositionWindow();
        };
    }

    private void RegisterIpcHandlers()
    {
        try
        {
            var router = App.ServiceProvider.GetService(typeof(IIpcMessageRouter)) as IIpcMessageRouter;
            if (router != null)
            {
                router.RegisterHandler(IpcMessageTypes.SwitchViewMode, msg =>
                {
                    Dispatcher.UIThread.Post(() => _viewModel?.SwitchViewMode());
                });
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[悬浮窗] 注册IPC处理器失败: {ex.Message}");
        }
    }

    private void PositionWindow()
    {
        if (_viewModel == null || !_isInitialized) return;

        var screen = Screens.Primary;
        if (screen == null) return;

        var workArea = screen.WorkingArea;

        var settingsService = App.ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
        var settings = settingsService?.GetSettings();
        if (settings == null) return;

        // 等待布局完成后获取实际窗口尺寸
        this.UpdateLayout();
        var windowWidth = this.Bounds.Width;
        var windowHeight = this.Bounds.Height;

        int x, y;
        switch (settings.Overlay.OverlayPosition)
        {
            case OverlayPosition.TopLeft:
                x = workArea.X + 20;
                y = workArea.Y + 20;
                break;
            case OverlayPosition.TopRight:
                x = workArea.X + workArea.Width - (int)windowWidth - 20;
                y = workArea.Y + 20;
                break;
            case OverlayPosition.BottomLeft:
                x = workArea.X + 20;
                y = workArea.Y + workArea.Height - (int)windowHeight - 20;
                break;
            case OverlayPosition.BottomRight:
            default:
                x = workArea.X + workArea.Width - (int)windowWidth - 20;
                y = workArea.Y + workArea.Height - (int)windowHeight - 20;
                break;
        }

        Position = new PixelPoint(x, y);
    }

    private void OnFrameTick(object? sender, EventArgs e)
    {
        _viewModel?.MarkFrame();
    }

    private void SetWindowTransparent()
    {
        if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
        {
            SetWindowExTransparent(handle.Handle);
        }
    }

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private void SetWindowExTransparent(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt32() | WS_EX_TRANSPARENT | WS_EX_LAYERED));
    }

    #endregion

    #region 拖拽处理

    private void OnDragHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStartPoint = e.GetPosition(this);
            _isDragging = true;
            
            if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
            {
                DisableClickThrough(handle.Handle);
            }
        }
    }

    private void OnDragHandleMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;
            Position = new PixelPoint(Position.X + (int)offset.X, Position.Y + (int)offset.Y);
        }
    }

    private void OnDragHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        
        if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
        {
            EnableClickThrough(handle.Handle);
        }
    }

    private void DisableClickThrough(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt32() & ~WS_EX_TRANSPARENT));
    }

    private void EnableClickThrough(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt32() | WS_EX_TRANSPARENT | WS_EX_LAYERED));
    }

    #endregion

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _frameTimer?.Stop();
        _frameTimer = null;
        base.OnClosing(e);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            ShowContextMenu(e.GetPosition(this));
            e.Handled = true;
        }
    }

    private void ShowContextMenu(Point position)
    {
        var menu = new ContextMenu
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2d2d2d")),
            Foreground = Avalonia.Media.Brushes.White,
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#444")),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(4)
        };

        var showMainWindowItem = new MenuItem
        {
            Header = "显示主窗口"
        };
        showMainWindowItem.Click += (s, e) => SendIpcMessage(IpcMessageTypes.ShowMainWindow);

        var openSettingsItem = new MenuItem
        {
            Header = "打开设置"
        };
        openSettingsItem.Click += (s, e) => SendIpcMessage(IpcMessageTypes.ShowSettings);

        var separator = new Separator();

        var exitItem = new MenuItem
        {
            Header = "退出"
        };
        exitItem.Click += (s, e) => SendIpcMessage(IpcMessageTypes.ExitApplication);

        menu.Items.Add(showMainWindowItem);
        menu.Items.Add(openSettingsItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);

        menu.Open(this);
    }

    private void SendIpcMessage(string messageType)
    {
        try
        {
            var ipcService = App.ServiceProvider.GetService(typeof(IIpcService)) as IIpcService;
            if (ipcService != null && ipcService.IsConnected)
            {
                _ = App.SendIpcMessageAsync(ipcService, messageType);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[悬浮窗] 发送IPC消息失败: {ex.Message}");
        }
    }
}