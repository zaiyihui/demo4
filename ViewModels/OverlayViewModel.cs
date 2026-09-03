using Avalonia.Threading;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace ComputerCompanion.ViewModels;

public enum OverlayViewMode
{
    Minimal,
    Standard,
    Complete
}

public partial class OverlayViewModel : ObservableObject, IDisposable
{
    private readonly IHardwareMonitorService _monitor;
    private readonly ILatencyMonitorService _latencyMonitor;
    private readonly Settings _settings;
    private bool _disposed;

    [ObservableProperty]
    private string _fpsText;

    [ObservableProperty]
    private string _gpuText;

    [ObservableProperty]
    private string _cpuText;

    [ObservableProperty]
    private string _memoryText;

    [ObservableProperty]
    private string _latencyText;
    
    [ObservableProperty]
    private string _overlayTextColor;

    [ObservableProperty]
    private string _fps1PercentLowText;

    [ObservableProperty]
    private string _frameTimeText;

    [ObservableProperty]
    private string _vramText;

    [ObservableProperty]
    private OverlayViewMode _currentViewMode;

    public bool ShowFps => true;
    public bool ShowFpsDetails => CurrentViewMode != OverlayViewMode.Minimal;
    public bool ShowGpu => CurrentViewMode != OverlayViewMode.Minimal;
    public bool ShowCpu => CurrentViewMode != OverlayViewMode.Minimal;
    public bool ShowMemory => CurrentViewMode == OverlayViewMode.Complete;
    public bool ShowLatency => CurrentViewMode == OverlayViewMode.Complete;
    public bool ShowVram => CurrentViewMode == OverlayViewMode.Complete && _monitor.HasGpu;

    public OverlayViewModel(
        IHardwareMonitorService monitor, 
        ILatencyMonitorService latencyMonitor,
        Settings settings)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _latencyMonitor = latencyMonitor ?? throw new ArgumentNullException(nameof(latencyMonitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        _monitor.DataUpdated += OnHardwareDataUpdated;
        _latencyMonitor.LatencyUpdated += OnLatencyUpdated;
        
        _fpsText = "--";
        _gpuText = "显示: --";
        _cpuText = "处理: --";
        _memoryText = "内存: --";
        _latencyText = "延迟: --";
        _overlayTextColor = settings.Overlay.OverlayTextColor;
        _fps1PercentLowText = "--";
        _frameTimeText = "--";
        _vramText = "--";
        _currentViewMode = OverlayViewMode.Standard;

        ApplySkinSettings();
    }

    private void ApplySkinSettings()
    {
        // 根据 settings.Overlay.SkinName 应用预设
        var skin = SkinService.GetBuiltInSkin(_settings.Overlay.SkinName);
        if (skin != null)
        {
            _overlayTextColor = skin.TextColor;
        }
    }

    public void SwitchViewMode()
    {
        var modes = Enum.GetValues<OverlayViewMode>();
        var currentIndex = Array.IndexOf(modes, CurrentViewMode);
        var nextIndex = (currentIndex + 1) % modes.Length;
        CurrentViewMode = modes[nextIndex];
        
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowFpsDetails)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowGpu)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowCpu)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowMemory)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowLatency)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowVram)));
    }

    public void MarkFrame()
    {
        _monitor.MarkFrame();
        
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => UpdateFpsDisplay());
            return;
        }
        
        UpdateFpsDisplay();
    }

    private void UpdateFpsDisplay()
    {
        if (_disposed)
            return;
            
        if (_monitor.Fps.HasValue)
        {
            FpsText = _monitor.Fps.Value > 0 ? _monitor.Fps.Value.ToString("0") : "--";
        }
    }

    private void OnHardwareDataUpdated()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(OnHardwareDataUpdated);
            return;
        }

        if (_disposed)
            return;

        if (_settings.Overlay.OverlayShowGpu && _monitor.HasGpu)
        {
            if (_monitor.GpuUsage.HasValue)
                GpuText = $"{_monitor.GpuUsage.Value:F0}%";
            else
                GpuText = "--";
        }
        else
        {
            GpuText = "--";
        }

        if (_settings.Overlay.OverlayShowCpu)
        {
            if (_monitor.CpuUsage.HasValue)
                CpuText = $"{_monitor.CpuUsage.Value:F0}%";
            else
                CpuText = "--";
        }
        else
        {
            CpuText = "--";
        }

        if (_settings.Overlay.OverlayShowMemory)
        {
            if (_monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue)
            {
                var usagePercent = (_monitor.MemoryUsed.Value / _monitor.MemoryTotal.Value) * 100;
                MemoryText = $"{usagePercent:F0}%";
            }
            else
            {
                MemoryText = "--";
            }
        }
        else
        {
            MemoryText = "--";
        }

        if (_monitor.Fps1PercentLow.HasValue)
        {
            Fps1PercentLowText = _monitor.Fps1PercentLow.Value > 0 ? $"1%: {_monitor.Fps1PercentLow.Value:F0}" : "--";
        }

        // 帧生成时间 (Frame Time)
        if (_monitor.FrameTimeMs.HasValue)
        {
            FrameTimeText = $"FT: {_monitor.FrameTimeMs.Value:F1}ms";
        }
        else
        {
            FrameTimeText = "--";
        }

        if (_monitor.HasGpu && _monitor.GpuVramUsed.HasValue && _monitor.GpuVramTotal.HasValue)
        {
            var vramPercent = (_monitor.GpuVramUsed.Value / _monitor.GpuVramTotal.Value) * 100;
            VramText = $"VRAM: {_monitor.GpuVramUsed.Value:F1}GB/{_monitor.GpuVramTotal.Value:F1}GB ({vramPercent:F0}%)";
        }
        else
        {
            VramText = "--";
        }
    }

    private void OnLatencyUpdated()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(OnLatencyUpdated);
            return;
        }

        if (_disposed)
            return;

        if (_settings.Overlay.OverlayShowLatency)
        {
            if (_latencyMonitor.NetworkLatency.HasValue)
            {
                LatencyText = $"LAT {_latencyMonitor.NetworkLatency.Value}ms";
            }
            else
            {
                LatencyText = "LAT: --";
            }
        }
        else
        {
            LatencyText = "LAT: --";
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        _monitor.DataUpdated -= OnHardwareDataUpdated;
        _latencyMonitor.LatencyUpdated -= OnLatencyUpdated;
        
        GC.SuppressFinalize(this);
    }
}