﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ComputerCompanion.ViewModels;

/// <summary>
/// 性能监控面板视图模型
/// </summary>
public partial class PerformanceDashboardViewModel : ObservableObject, IDisposable
{
    private readonly IPerformanceMonitorService _monitor;
    private readonly IHardwareMonitorService _hardware;
    private readonly ILogService _log;
    private readonly IAlertSoundService? _alertSoundService;
    private readonly IThemeService? _themeService;
    private readonly IDataExportService? _dataExportService;
    private bool _disposed;

    // 时间范围
    [ObservableProperty]
    private string _selectedTimeRange = "1小时";

    // 实时指标
    [ObservableProperty]
    private double _cpuUsage;

    [ObservableProperty]
    private double _cpuTemperature;

    [ObservableProperty]
    private double _gpuUsage;

    [ObservableProperty]
    private double _gpuTemperature;

    [ObservableProperty]
    private double _memoryUsage;

    [ObservableProperty]
    private double _memoryUsedGB;

    [ObservableProperty]
    private double _memoryTotalGB;

    [ObservableProperty]
    private double _fps;

    [ObservableProperty]
    private double _avgResponseTime;

    [ObservableProperty]
    private int _errorsPerMinute;

    // 历史数据
    public ObservableCollection<MetricDataPointViewModel> CpuHistory { get; } = new();
    public ObservableCollection<MetricDataPointViewModel> GpuHistory { get; } = new();
    public ObservableCollection<MetricDataPointViewModel> MemoryHistory { get; } = new();
    public ObservableCollection<MetricDataPointViewModel> FpsHistory { get; } = new();

    // 告警列表
    public ObservableCollection<AlertViewModel> ActiveAlerts { get; } = new();

    // 图表数据点
    public ObservableCollection<ChartPoint> CpuChartPoints { get; } = new();
    public ObservableCollection<ChartPoint> GpuChartPoints { get; } = new();
    public ObservableCollection<ChartPoint> MemoryChartPoints { get; } = new();

    // 指标过滤
    [ObservableProperty]
    private bool _showCpu = true;

    [ObservableProperty]
    private bool _showGpu = true;

    [ObservableProperty]
    private bool _showMemory = true;

    [ObservableProperty]
    private bool _showFps = true;

    // 系统状态
    [ObservableProperty]
    private string _systemStatus = "正常";

    [ObservableProperty]
    private string _statusColor = "#00b894";

    // 最后更新时间
    [ObservableProperty]
    private DateTime _lastUpdate = DateTime.Now;

    public PerformanceDashboardViewModel(
        IPerformanceMonitorService monitor,
        IHardwareMonitorService hardware,
        ILogService log,
        IAlertSoundService? alertSoundService = null,
        IThemeService? themeService = null,
        IDataExportService? dataExportService = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _alertSoundService = alertSoundService;
        _themeService = themeService;
        _dataExportService = dataExportService;

        // 订阅性能监控事件
        _monitor.MetricsUpdated += OnMetricsUpdated;
        _monitor.AlertTriggered += OnAlertTriggered;

        // 初始化数据
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _monitor.InitializeAsync();
            await _monitor.StartAsync();

            // 加载历史数据
            LoadHistoricalData();

            _log.Info("[性能面板] 已初始化");
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 初始化失败", ex);
        }
    }

    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnMetricsUpdated(sender, e));
            return;
        }

        if (_disposed)
            return;

        try
        {
            var metrics = e.Metrics;

            // 更新实时指标
            CpuUsage = metrics.CpuUsagePercent;
            CpuTemperature = metrics.CpuTemperature;
            GpuUsage = metrics.GpuUsagePercent;
            GpuTemperature = metrics.GpuTemperature;
            MemoryUsage = metrics.MemoryUsagePercent;
            MemoryUsedGB = metrics.MemoryUsedMB / 1024.0;
            MemoryTotalGB = metrics.MemoryTotalMB / 1024.0;
            Fps = metrics.Fps;
            AvgResponseTime = metrics.AverageResponseTimeMs;
            ErrorsPerMinute = metrics.ErrorsPerMinute;

            // 更新图表数据
            UpdateChartData(metrics);

            // 更新系统状态
            UpdateSystemStatus(metrics);

            // 处理告警
            if (e.TriggeredAlerts != null)
            {
                foreach (var alert in e.TriggeredAlerts)
                {
                    AddAlert(alert);
                }
            }

            LastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 更新指标失败", ex);
        }
    }

    private void OnAlertTriggered(object? sender, AlertTriggeredEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnAlertTriggered(sender, e));
            return;
        }

        AddAlert(e);
    }

    private void AddAlert(AlertTriggeredEventArgs alert)
    {
        ActiveAlerts.Insert(0, new AlertViewModel
        {
            RuleName = alert.Rule.Name,
            Severity = alert.Rule.Severity.ToString(),
            CurrentValue = alert.CurrentValue,
            Threshold = alert.Rule.Threshold,
            TriggeredAt = alert.TriggeredAt,
            Message = $"{alert.Rule.Name}: {alert.CurrentValue:F1} (阈值: {alert.Rule.Threshold})"
        });

        // 限制告警数量
        while (ActiveAlerts.Count > 20)
        {
            ActiveAlerts.RemoveAt(ActiveAlerts.Count - 1);
        }
    }

    private void UpdateChartData(PerformanceMetrics metrics)
    {
        var now = DateTime.Now;

        // CPU 图表
        CpuChartPoints.Add(new ChartPoint { Time = now, Value = metrics.CpuUsagePercent });
        if (CpuChartPoints.Count > 60)
            CpuChartPoints.RemoveAt(0);

        // GPU 图表
        GpuChartPoints.Add(new ChartPoint { Time = now, Value = metrics.GpuUsagePercent });
        if (GpuChartPoints.Count > 60)
            GpuChartPoints.RemoveAt(0);

        // 内存图表
        MemoryChartPoints.Add(new ChartPoint { Time = now, Value = metrics.MemoryUsagePercent });
        if (MemoryChartPoints.Count > 60)
            MemoryChartPoints.RemoveAt(0);
    }

    private void UpdateSystemStatus(PerformanceMetrics metrics)
    {
        // 根据各项指标评估系统状态
        if (metrics.CpuUsagePercent > 90 || metrics.MemoryUsagePercent > 90 || metrics.CpuTemperature > 85)
        {
            SystemStatus = "警告";
            StatusColor = "#fdcb6e";
        }
        else if (metrics.CpuUsagePercent > 80 || metrics.MemoryUsagePercent > 80 || metrics.CpuTemperature > 75)
        {
            SystemStatus = "注意";
            StatusColor = "#ffeaa7";
        }
        else
        {
            SystemStatus = "正常";
            StatusColor = "#00b894";
        }
    }

    private void LoadHistoricalData()
    {
        try
        {
            var duration = GetTimeRangeDuration();

            // 加载CPU历史数据
            var cpuData = _monitor.GetHistoricalMetrics("CpuUsagePercent", duration);
            foreach (var point in cpuData.TakeLast(60))
            {
                CpuHistory.Add(new MetricDataPointViewModel
                {
                    Timestamp = point.Timestamp,
                    Value = point.Value
                });
            }

            // 加载GPU历史数据
            var gpuData = _monitor.GetHistoricalMetrics("GpuUsagePercent", duration);
            foreach (var point in gpuData.TakeLast(60))
            {
                GpuHistory.Add(new MetricDataPointViewModel
                {
                    Timestamp = point.Timestamp,
                    Value = point.Value
                });
            }

            // 加载内存历史数据
            var memoryData = _monitor.GetHistoricalMetrics("MemoryUsagePercent", duration);
            foreach (var point in memoryData.TakeLast(60))
            {
                MemoryHistory.Add(new MetricDataPointViewModel
                {
                    Timestamp = point.Timestamp,
                    Value = point.Value
                });
            }
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 加载历史数据失败", ex);
        }
    }

    private TimeSpan GetTimeRangeDuration()
    {
        return SelectedTimeRange switch
        {
            "15分钟" => TimeSpan.FromMinutes(15),
            "30分钟" => TimeSpan.FromMinutes(30),
            "1小时" => TimeSpan.FromHours(1),
            "6小时" => TimeSpan.FromHours(6),
            "24小时" => TimeSpan.FromHours(24),
            _ => TimeSpan.FromHours(1)
        };
    }

    [RelayCommand]
    public async Task RefreshDataAsync()
    {
        try
        {
            CpuHistory.Clear();
            GpuHistory.Clear();
            MemoryHistory.Clear();
            CpuChartPoints.Clear();
            GpuChartPoints.Clear();
            MemoryChartPoints.Clear();

            LoadHistoricalData();

            _log.Info("[性能面板] 数据已刷新");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 刷新数据失败", ex);
        }
    }

    [RelayCommand]
    public async Task ExportDataAsync()
    {
        try
        {
            if (_dataExportService != null)
            {
                var exportPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ComputerCompanion",
                    $"performance_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );

                var allData = new List<MetricDataPoint>();
                
                // 收集CPU数据
                foreach (var point in CpuHistory)
                {
                    allData.Add(new MetricDataPoint
                    {
                        Timestamp = point.Timestamp,
                        Value = point.Value,
                        Unit = "%",
                        MetricType = "CpuUsagePercent"
                    });
                }

                // 收集内存数据
                foreach (var point in MemoryHistory)
                {
                    allData.Add(new MetricDataPoint
                    {
                        Timestamp = point.Timestamp,
                        Value = point.Value,
                        Unit = "%",
                        MetricType = "MemoryUsagePercent"
                    });
                }

                _dataExportService.ExportToCsv(allData, exportPath);
                _log.Info($"[性能面板] 数据已导出到: {exportPath}");
            }
            else
            {
                _log.Warn("[性能面板] 数据导出服务未初始化");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 导出数据失败", ex);
        }
    }

    public void SetTimeRange(string timeRange)
    {
        SelectedTimeRange = timeRange;
        _ = RefreshDataAsync();
    }

    public void ToggleMetric(string metric, bool enabled)
    {
        switch (metric)
        {
            case "CPU":
                ShowCpu = enabled;
                break;
            case "GPU":
                ShowGpu = enabled;
                break;
            case "Memory":
                ShowMemory = enabled;
                break;
            case "FPS":
                ShowFps = enabled;
                break;
        }
    }

    public void ToggleTheme()
    {
        try
        {
            if (_themeService != null)
            {
                _themeService.ToggleTheme();
                _log.Info("[性能面板] 主题已切换");
            }
            else
            {
                _log.Warn("[性能面板] 主题服务未初始化");
            }
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 切换主题失败", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _monitor.MetricsUpdated -= OnMetricsUpdated;
        _monitor.AlertTriggered -= OnAlertTriggered;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 指标数据点视图模型
/// </summary>
public class MetricDataPointViewModel
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// 图表数据点
/// </summary>
public class ChartPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// 告警视图模型
/// </summary>
public class AlertViewModel
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double Threshold { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Message { get; set; } = string.Empty;

    public string SeverityColor => Severity switch
    {
        "Critical" => "#e74c3c",
        "Error" => "#e67e22",
        "Warning" => "#f39c12",
        _ => "#3498db"
    };

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.Now - TriggeredAt;
            if (diff.TotalMinutes < 1)
                return "刚刚";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}分钟前";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}小时前";
            return $"{(int)diff.TotalDays}天前";
        }
    }
}
