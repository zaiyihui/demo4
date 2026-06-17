using Avalonia.Threading;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace ComputerCompanion.ViewModels;

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