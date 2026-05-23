using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Threading;
using ComputerCompanion.ViewModels;
using System;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Views;

public partial class OverlayWindow : Window
{
    private OverlayViewModel? _viewModel;
    private DispatcherTimer? _frameTimer;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void Initialize(OverlayViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        // 设置鼠标穿透
        SetWindowTransparent();
        
        // 启动帧计数器模拟定时器
        _frameTimer = new DispatcherTimer();
        _frameTimer.Interval = TimeSpan.FromMilliseconds(16); // 约 60 FPS
        _frameTimer.Tick += OnFrameTick;
        _frameTimer.Start();
    }

    private void OnFrameTick(object? sender, EventArgs e)
    {
        _viewModel?.IncrementFrameCount();
    }

    private void SetWindowTransparent()
    {
        // Windows 平台设置鼠标穿透
        if (OperatingSystem.IsWindows())
        {
            var handle = TryGetPlatformHandle();
            if (handle != null)
            {
                SetWindowExTransparent(handle.Handle);
            }
        }
    }

    #region Win32 API for mouse transparency

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    private void SetWindowExTransparent(IntPtr hwnd)
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
