using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;

namespace ComputerCompanion.ViewModels;

/// <summary>
/// NVIDIA 风格悬浮窗视图模型
/// 用于在游戏中显示硬件性能数据
/// </summary>
public partial class OverlayViewModel : ObservableObject
{
    #region 私有字段

    private readonly HardwareMonitorService _monitor;
    private readonly Settings _settings;
    private DateTime _lastUpdateTime;
    private int _frameCount;
    private float _lastFps;

    #endregion

    #region 构造函数

    public OverlayViewModel(HardwareMonitorService monitor, Settings settings)
    {
        _monitor = monitor;
        _settings = settings;
        
        // 订阅数据更新事件
        _monitor.DataUpdated += OnDataUpdated;
        _lastUpdateTime = DateTime.Now;
        _frameCount = 0;
        
        // 初始化默认值
        _fpsText = "N/A";
        _gpuText = "GPU: --";
        _cpuText = "CPU: --";
        _memoryText = "MEM: --";
        _latencyText = "N/A";
        _overlayTextColor = settings.OverlayTextColor;
    }

    #endregion

    #region 可观察属性

    /// <summary>
    /// FPS 显示文本
    /// </summary>
    [ObservableProperty]
    private string _fpsText;

    /// <summary>
    /// GPU 显示文本
    /// </summary>
    [ObservableProperty]
    private string _gpuText;

    /// <summary>
    /// CPU 显示文本
    /// </summary>
    [ObservableProperty]
    private string _cpuText;

    /// <summary>
    /// 内存显示文本
    /// </summary>
    [ObservableProperty]
    private string _memoryText;

    /// <summary>
    /// 延迟显示文本
    /// </summary>
    [ObservableProperty]
    private string _latencyText;
    
    /// <summary>
    /// 悬浮窗文字颜色
    /// </summary>
    [ObservableProperty]
    private string _overlayTextColor;

    #endregion

    #region 公共方法

    /// <summary>
    /// 帧计数器增加（模拟 FPS 测量）
    /// </summary>
    public void IncrementFrameCount()
    {
        _frameCount++;
        
        var now = DateTime.Now;
        var elapsed = (now - _lastUpdateTime).TotalSeconds;
        
        if (elapsed >= 1.0)
        {
            _lastFps = _frameCount / (float)elapsed;
            FpsText = _lastFps > 0 ? _lastFps.ToString("0") : "N/A";
            _frameCount = 0;
            _lastUpdateTime = now;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 数据更新事件处理
    /// </summary>
    private void OnDataUpdated()
    {
        // 构建 GPU 信息 (NVIDIA 风格)
        var gpuParts = new System.Collections.Generic.List<string>();
        if (_settings.OverlayShowGpu && _monitor.HasGpu)
        {
            if (_monitor.GpuUsage.HasValue)
                gpuParts.Add($"GPU {_monitor.GpuUsage.Value:F0}%");
            if (_monitor.GpuTemp.HasValue)
                gpuParts.Add($"{_monitor.GpuTemp.Value:F0}°C");
            if (_monitor.GpuClock.HasValue)
                gpuParts.Add($"{_monitor.GpuClock.Value:F0} MHz");
            
            GpuText = string.Join(" | ", gpuParts);
        }
        else
        {
            GpuText = "GPU: --";
        }

        // 构建 CPU 信息
        var cpuParts = new System.Collections.Generic.List<string>();
        if (_settings.OverlayShowCpu)
        {
            if (_monitor.CpuUsage.HasValue)
                cpuParts.Add($"CPU {_monitor.CpuUsage.Value:F0}%");
            
            CpuText = string.Join(" | ", cpuParts);
        }
        else
        {
            CpuText = "CPU: --";
        }

        // 构建内存信息
        if (_settings.OverlayShowMemory)
        {
            if (_monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue)
            {
                var usagePercent = (_monitor.MemoryUsed.Value / _monitor.MemoryTotal.Value) * 100;
                MemoryText = $"MEM {usagePercent:F0}%";
            }
            else
            {
                MemoryText = "MEM: --";
            }
        }
        else
        {
            MemoryText = "MEM: --";
        }

        // 延迟（暂时显示为 N/A）
        LatencyText = "N/A";
    }

    #endregion
}
