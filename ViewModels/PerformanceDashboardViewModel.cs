﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ComputerCompanion.ViewModels;

public partial class PerformanceDashboardViewModel : ObservableObject, IDisposable
{
    private readonly IPerformanceMonitorService _monitor;
    private readonly IHardwareMonitorService _hardware;
    private readonly ILogService _log;
    private readonly IAlertSoundService? _alertSoundService;
    private readonly IThemeService? _themeService;
    private readonly IDataExportService? _dataExportService;
    private readonly IChartService? _chartService;
    private readonly IPerformanceLoggerService? _performanceLogger;
    private bool _disposed;

    private const int MaxChartPoints = 60;
    private const int MaxAlertItems = 20;

    [ObservableProperty]
    private string _selectedTimeRange = "1小时";

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

    public ObservableCollection<MetricDataPointViewModel> CpuHistory { get; } = new();
    public ObservableCollection<MetricDataPointViewModel> GpuHistory { get; } = new();
    public ObservableCollection<MetricDataPointViewModel> MemoryHistory { get; } = new();
    public ObservableCollection<MetricDataPointViewModel> FpsHistory { get; } = new();

    public ObservableCollection<AlertViewModel> ActiveAlerts { get; } = new();

    public ObservableCollection<ChartPoint> CpuChartPoints { get; } = new();
    public ObservableCollection<ChartPoint> GpuChartPoints { get; } = new();
    public ObservableCollection<ChartPoint> MemoryChartPoints { get; } = new();

    public ISeries[] CpuSeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] GpuSeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] MemorySeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] DefaultXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] DefaultYAxes { get; private set; } = Array.Empty<Axis>();

    private void InitializeCharts()
    {
        CpuSeries = new ISeries[]
        {
            new LineSeries<ChartPoint>
            {
                Name = "CPU",
                Values = CpuChartPoints,
                Stroke = new SolidColorPaint(new SKColor(78, 205, 196)) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 4,
                LineSmoothness = 0.8
            }
        };

        GpuSeries = new ISeries[]
        {
            new LineSeries<ChartPoint>
            {
                Name = "GPU",
                Values = GpuChartPoints,
                Stroke = new SolidColorPaint(new SKColor(162, 155, 254)) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 4,
                LineSmoothness = 0.8
            }
        };

        MemorySeries = new ISeries[]
        {
            new LineSeries<ChartPoint>
            {
                Name = "内存",
                Values = MemoryChartPoints,
                Stroke = new SolidColorPaint(new SKColor(253, 121, 168)) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 4,
                LineSmoothness = 0.8
            }
        };

        DefaultXAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => DateTime.FromOADate(value).ToString("mm:ss"),
                MinLimit = DateTime.Now.AddSeconds(-60).ToOADate(),
                MaxLimit = DateTime.Now.ToOADate()
            }
        };

        DefaultYAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => $"{value:F0}%",
                MinLimit = 0,
                MaxLimit = 100
            }
        };
    }

    [ObservableProperty]
    private string _selectedMetricCard = "CPU";

    [ObservableProperty]
    private bool _showCpu = true;

    [ObservableProperty]
    private bool _showGpu = true;

    [ObservableProperty]
    private bool _showMemory = true;

    [ObservableProperty]
    private bool _showFps = true;

    [ObservableProperty]
    private string _systemStatus = "正常";

    [ObservableProperty]
    private string _statusColor = "#00b894";

    [ObservableProperty]
    private bool _isRecording = false;

    [ObservableProperty]
    private string _logFilePath = "";

    [ObservableProperty]
    private DateTime _lastUpdate = DateTime.Now;

    public PerformanceDashboardViewModel(
        IPerformanceMonitorService monitor,
        IHardwareMonitorService hardware,
        ILogService log,
        IAlertSoundService? alertSoundService = null,
        IThemeService? themeService = null,
        IDataExportService? dataExportService = null,
        IChartService? chartService = null,
        IPerformanceLoggerService? performanceLogger = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _alertSoundService = alertSoundService;
        _themeService = themeService;
        _dataExportService = dataExportService;
        _chartService = chartService;
        _performanceLogger = performanceLogger;

        _monitor.MetricsUpdated += OnMetricsUpdated;
        _monitor.AlertTriggered += OnAlertTriggered;
        _hardware.DataUpdated += OnHardwareDataUpdated;

        InitializeCharts();

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _monitor.InitializeAsync();
            await _monitor.StartAsync();

            LoadHistoricalData();

            _log.Info("[性能面板] 已初始化");
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 初始化失败", ex);
        }
    }

    private void OnHardwareDataUpdated()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(OnHardwareDataUpdated);
            return;
        }

        if (_disposed) return;

        try
        {
            if (_hardware.CpuTemp.HasValue)
                CpuTemperature = _hardware.CpuTemp.Value;

            if (_hardware.GpuTemp.HasValue)
                GpuTemperature = _hardware.GpuTemp.Value;

            if (_hardware.GpuUsage.HasValue)
                GpuUsage = _hardware.GpuUsage.Value;

            if (_hardware.GpuVramUsed.HasValue && _hardware.GpuVramTotal.HasValue)
            {
                var gpuUsagePercent = (_hardware.GpuVramUsed.Value / _hardware.GpuVramTotal.Value) * 100;
                if (Math.Abs(GpuUsage - gpuUsagePercent) > 1)
                {
                    GpuUsage = gpuUsagePercent;
                }
            }

            if (_hardware.Fps.HasValue)
                Fps = _hardware.Fps.Value;
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 更新硬件数据失败", ex);
        }
    }

    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnMetricsUpdated(sender, e));
            return;
        }

        if (_disposed) return;

        try
        {
            var metrics = e.Metrics;

            UpdateMetricIfChanged(ref _cpuUsage, metrics.CpuUsagePercent, nameof(CpuUsage));
            UpdateMetricIfChanged(ref _memoryUsage, metrics.MemoryUsagePercent, nameof(MemoryUsage));
            UpdateMetricIfChanged(ref _memoryUsedGB, metrics.MemoryUsedMB / 1024.0, nameof(MemoryUsedGB));
            UpdateMetricIfChanged(ref _memoryTotalGB, metrics.MemoryTotalMB / 1024.0, nameof(MemoryTotalGB));
            UpdateMetricIfChanged(ref _avgResponseTime, metrics.AverageResponseTimeMs, nameof(AvgResponseTime));
            UpdateMetricIfChanged(ref _errorsPerMinute, metrics.ErrorsPerMinute, nameof(ErrorsPerMinute));
            UpdateMetricIfChanged(ref _fps, metrics.Fps, nameof(Fps));

            UpdateChartData(metrics);
            UpdateSystemStatus(metrics);

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

    private void UpdateMetricIfChanged<T>(ref T field, T newValue, string propertyName) where T : struct, IComparable<T>
    {
        if (!field.Equals(newValue))
        {
            field = newValue;
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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
        var existingAlert = ActiveAlerts.FirstOrDefault(
            a => a.RuleName == alert.Rule.Name && 
                 Math.Abs(a.CurrentValue - alert.CurrentValue) < 1 &&
                 (DateTime.Now - a.TriggeredAt).TotalSeconds < 30);

        if (existingAlert != null)
        {
            existingAlert.CurrentValue = alert.CurrentValue;
            existingAlert.TriggeredAt = alert.TriggeredAt;
            return;
        }

        ActiveAlerts.Insert(0, new AlertViewModel
        {
            RuleName = alert.Rule.Name,
            Severity = alert.Rule.Severity.ToString(),
            CurrentValue = alert.CurrentValue,
            Threshold = alert.Rule.Threshold,
            TriggeredAt = alert.TriggeredAt,
            Message = $"{alert.Rule.Name}: {alert.CurrentValue:F1} (阈值: {alert.Rule.Threshold})"
        });

        while (ActiveAlerts.Count > MaxAlertItems)
        {
            ActiveAlerts.RemoveAt(ActiveAlerts.Count - 1);
        }

        _alertSoundService?.PlayAlertSound(alert.Rule.Severity);
    }

    private void UpdateChartData(PerformanceMetrics metrics)
    {
        var now = DateTime.Now;

        UpdateChartPoints(CpuChartPoints, metrics.CpuUsagePercent, now);
        UpdateChartPoints(GpuChartPoints, metrics.GpuUsagePercent, now);
        UpdateChartPoints(MemoryChartPoints, metrics.MemoryUsagePercent, now);
    }

    private void UpdateChartPoints(ObservableCollection<ChartPoint> points, double value, DateTime time)
    {
        ChartPoint point;

        if (_chartService != null)
        {
            point = _chartService.GetChartPoint();
            point.Time = time;
            point.Value = value;
        }
        else
        {
            point = new ChartPoint { Time = time, Value = value };
        }

        points.Add(point);

        while (points.Count > MaxChartPoints)
        {
            var removedPoint = points[0];
            points.RemoveAt(0);
            _chartService?.ReturnChartPoint(removedPoint);
        }
    }

    private void UpdateSystemStatus(PerformanceMetrics metrics)
    {
        string newStatus;
        string newColor;

        if (metrics.CpuUsagePercent > 90 || metrics.MemoryUsagePercent > 90 || metrics.CpuTemperature > 85)
        {
            newStatus = "警告";
            newColor = "#fdcb6e";
        }
        else if (metrics.CpuUsagePercent > 80 || metrics.MemoryUsagePercent > 80 || metrics.CpuTemperature > 75)
        {
            newStatus = "注意";
            newColor = "#ffeaa7";
        }
        else
        {
            newStatus = "正常";
            newColor = "#00b894";
        }

        if (SystemStatus != newStatus)
            SystemStatus = newStatus;

        if (StatusColor != newColor)
            StatusColor = newColor;
    }

    private void LoadHistoricalData()
    {
        try
        {
            var duration = GetTimeRangeDuration();

            LoadHistoricalDataForMetric("CpuUsagePercent", CpuHistory, duration);
            LoadHistoricalDataForMetric("GpuUsagePercent", GpuHistory, duration);
            LoadHistoricalDataForMetric("MemoryUsagePercent", MemoryHistory, duration);
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 加载历史数据失败", ex);
        }
    }

    private void LoadHistoricalDataForMetric(string metricName, ObservableCollection<MetricDataPointViewModel> collection, TimeSpan duration)
    {
        var data = _monitor.GetHistoricalMetrics(metricName, duration);
        
        foreach (var point in data.TakeLast(MaxChartPoints))
        {
            MetricDataPointViewModel vm;

            if (_chartService != null)
            {
                vm = _chartService.GetMetricDataPoint();
                vm.Timestamp = point.Timestamp;
                vm.Value = point.Value;
            }
            else
            {
                vm = new MetricDataPointViewModel
                {
                    Timestamp = point.Timestamp,
                    Value = point.Value
                };
            }

            collection.Add(vm);
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
    public void SelectMetricCard(string cardName)
    {
        SelectedMetricCard = cardName;
    }

    [RelayCommand]
    public async Task RefreshDataAsync()
    {
        try
        {
            ClearCollections();
            LoadHistoricalData();

            _log.Info("[性能面板] 数据已刷新");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 刷新数据失败", ex);
        }
    }

    private void ClearCollections()
    {
        ClearCollection(CpuHistory);
        ClearCollection(GpuHistory);
        ClearCollection(MemoryHistory);
        ClearCollection(FpsHistory);

        ClearChartCollection(CpuChartPoints);
        ClearChartCollection(GpuChartPoints);
        ClearChartCollection(MemoryChartPoints);
    }

    private void ClearCollection(ObservableCollection<MetricDataPointViewModel> collection)
    {
        if (_chartService != null)
        {
            foreach (var item in collection)
            {
                _chartService.ReturnMetricDataPoint(item);
            }
        }
        collection.Clear();
    }

    private void ClearChartCollection(ObservableCollection<ChartPoint> collection)
    {
        if (_chartService != null)
        {
            foreach (var item in collection)
            {
                _chartService.ReturnChartPoint(item);
            }
        }
        collection.Clear();
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

                foreach (var point in CpuHistory)
                {
                    allData.Add(new MetricDataPoint
                    {
                        Timestamp = point.Timestamp,
                        Value = point.Value,
                        Unit = "%",
                        MetricType = MetricType.Gauge
                    });
                }

                foreach (var point in MemoryHistory)
                {
                    allData.Add(new MetricDataPoint
                    {
                        Timestamp = point.Timestamp,
                        Value = point.Value,
                        Unit = "%",
                        MetricType = MetricType.Gauge
                    });
                }

                _dataExportService.ExportToCsv(allData, exportPath);
                _log.Info($"[性能面板] 数据已导出到: {exportPath}");
            }
            else
            {
                _log.Warning("[性能面板] 数据导出服务未初始化");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 导出数据失败", ex);
        }
    }

    [RelayCommand]
    public void ToggleRecording()
    {
        try
        {
            if (_performanceLogger == null)
            {
                _log.Warning("[性能面板] 性能日志服务未初始化");
                return;
            }

            if (_isRecording)
            {
                _performanceLogger.StopRecording();
                IsRecording = false;
                LogFilePath = "";
                _log.Info("[性能面板] 性能日志录制已停止");
            }
            else
            {
                _performanceLogger.StartRecording();
                IsRecording = true;
                LogFilePath = _performanceLogger.LogFilePath;
                _log.Info($"[性能面板] 性能日志录制已启动: {_performanceLogger.LogFilePath}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 切换录制状态失败", ex);
        }
    }

    public void SetTimeRange(string timeRange)
    {
        if (SelectedTimeRange != timeRange)
        {
            SelectedTimeRange = timeRange;
            _ = RefreshDataAsync();
        }
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
                _log.Warning("[性能面板] 主题服务未初始化");
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
        _hardware.DataUpdated -= OnHardwareDataUpdated;

        ClearCollections();

        GC.SuppressFinalize(this);
    }
}

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