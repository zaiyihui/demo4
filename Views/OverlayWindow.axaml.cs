using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ComputerCompanion.Models;
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

    public OverlayWindow()
    {
        InitializeComponent();
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
        
        PositionWindow();
    }

    private void PositionWindow()
    {
        if (_viewModel == null) return;
        
        var screen = Screens.Primary;
        if (screen == null) return;
        
        var workArea = screen.WorkingArea;
        
        var settings = App.SettingsService?.GetSettings();
        if (settings == null) return;
        
        int x, y;
        switch (settings.OverlayPosition)
        {
            case OverlayPosition.TopLeft:
                x = workArea.X + 20;
                y = workArea.Y + 20;
                break;
            case OverlayPosition.TopRight:
                x = workArea.X + workArea.Width - (int)Width - 20;
                y = workArea.Y + 20;
                break;
            case OverlayPosition.BottomLeft:
                x = workArea.X + 20;
                y = workArea.Y + workArea.Height - (int)Height - 20;
                break;
            case OverlayPosition.BottomRight:
            default:
                x = workArea.X + workArea.Width - (int)Width - 20;
                y = workArea.Y + workArea.Height - (int)Height - 20;
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
}