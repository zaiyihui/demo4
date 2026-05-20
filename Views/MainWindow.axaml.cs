using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using ComputerCompanion.Helpers;
using ComputerCompanion.ViewModels;
using System;

namespace ComputerCompanion.Views;

public partial class MainWindow : Window
{
    private Point _startPoint;
    private bool _isClickThroughEnabled = false;

    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        Opened += OnWindowOpened;
        KeyDown += OnKeyDown;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        EnableClickThrough();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _startPoint = e.GetPosition(this);
            DisableClickThrough();
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
        EnableClickThrough();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.F1)
        {
            ToggleClickThrough();
        }
        else if (e.Key == Key.F2)
        {
            ToggleGameMode();
        }
    }

    private void ToggleGameMode()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ToggleGameMode();
        }
    }

    private void EnableClickThrough()
    {
        if (!_isClickThroughEnabled)
        {
            _isClickThroughEnabled = true;
            SetClickThrough(true);
        }
    }

    private void DisableClickThrough()
    {
        if (_isClickThroughEnabled)
        {
            _isClickThroughEnabled = false;
            SetClickThrough(false);
        }
    }

    private void ToggleClickThrough()
    {
        if (_isClickThroughEnabled)
        {
            DisableClickThrough();
        }
        else
        {
            EnableClickThrough();
        }
    }

    private void SetClickThrough(bool enable)
    {
        if (OperatingSystem.IsWindows())
        {
            var hwnd = GetWindowHandle();
            if (hwnd != IntPtr.Zero)
            {
                if (enable)
                {
                    WindowHelper.EnableClickThrough(hwnd);
                }
                else
                {
                    WindowHelper.DisableClickThrough(hwnd);
                }
            }
        }
    }

    private IntPtr GetWindowHandle()
    {
        var handle = TryGetPlatformHandle();
        return handle != null ? handle.Handle : IntPtr.Zero;
    }
}
