using Avalonia.Controls;
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
using System.Timers;

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
    private readonly Timer? _alertCleanupTimer;
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
    public ObservableCollection<ChartPoint> FpsChartPoints { get; } = new();

    public ISeries[] CpuSeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] GpuSeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] MemorySeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] FpsSeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] DefaultXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] DefaultYAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] FpsYAxes { get; private set; } = Array.Empty<Axis>();

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

        FpsSeries = new ISeries[]
        {
            new LineSeries<ChartPoint>
            {
                Name = "FPS",
                Values = FpsChartPoints,
                Stroke = new SolidColorPaint(new SKColor(118, 185, 0)) { StrokeThickness = 2 },
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

        FpsYAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => $"{value:F0}",
                MinLimit = 0,
                MaxLimit = 240
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
    private DateTime _lastUpdate = DateTime.Now;

    public PerformanceDashboardViewModel(
        IPerformanceMonitorService monitor,
        IHardwareMonitorService hardware,
        ILogService log,
        IAlertSoundService? alertSoundService = null,
        IThemeService? themeService = null,
        IDataExportService? dataExportService = null,
        IChartService? chartService = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _alertSoundService = alertSoundService;
        _themeService = themeService;
        _dataExportService = dataExportService;
        _chartService = chartService;

        _monitor.MetricsUpdated += OnMetricsUpdated;
        _monitor.AlertTriggered += OnAlertTriggered;
        _hardware.DataUpdated += OnHardwareDataUpdated;

        InitializeCharts();

        _alertCleanupTimer = new Timer(60000);
        _alertCleanupTimer.Elapsed += OnAlertCleanupTimerElapsed;
        _alertCleanupTimer.AutoReset = true;
        _alertCleanupTimer.Start();

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

            Fps = _hardware.Fps.HasValue ? _hardware.Fps.Value : -1;
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
        CleanupExpiredAlerts();

        var existingAlert = ActiveAlerts.FirstOrDefault(
            a => a.RuleName == alert.Rule.Name);

        if (existingAlert != null)
        {
            existingAlert.CurrentValue = alert.CurrentValue;
            existingAlert.TriggeredAt = alert.TriggeredAt;
            existingAlert.Message = $"{alert.Rule.Name}: {alert.CurrentValue:F1} (阈值: {alert.Rule.Threshold})";
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

    private void CleanupExpiredAlerts()
    {
        var expiredAlerts = ActiveAlerts.Where(
            a => (DateTime.Now - a.TriggeredAt).TotalMinutes > 5).ToList();

        foreach (var alert in expiredAlerts)
        {
            ActiveAlerts.Remove(alert);
        }
    }

    private void OnAlertCleanupTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        CleanupExpiredAlerts();
    }

    private void UpdateChartData(PerformanceMetrics metrics)
    {
        var now = DateTime.Now;

        UpdateChartPoints(CpuChartPoints, metrics.CpuUsagePercent, now);
        UpdateChartPoints(GpuChartPoints, metrics.GpuUsagePercent, now);
        UpdateChartPoints(MemoryChartPoints, metrics.MemoryUsagePercent, now);
        
        if (metrics.Fps >= 0)
        {
            UpdateChartPoints(FpsChartPoints, metrics.Fps, now);
        }
    }

    private void UpdateChartPoints(ObservableCollection<ChartPoint> points, double value, DateTime time)
    {
        var point = _chartService != null
            ? _chartService.GetChartPoint()
            : new ChartPoint();

        point.Time = time;
        point.Value = value;

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
        var (newStatus, newColor) = GetSystemStatus(metrics);

        if (SystemStatus != newStatus)
            SystemStatus = newStatus;

        if (StatusColor != newColor)
            StatusColor = newColor;
    }

    private (string Status, string Color) GetSystemStatus(PerformanceMetrics metrics)
    {
        if (metrics.CpuUsagePercent > 90 || metrics.MemoryUsagePercent > 90 || metrics.CpuTemperature > 85)
        {
            return ("警告", "#fdcb6e");
        }

        if (metrics.CpuUsagePercent > 80 || metrics.MemoryUsagePercent > 80 || metrics.CpuTemperature > 75)
        {
            return ("注意", "#ffeaa7");
        }

        return ("正常", "#00b894");
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
            var vm = _chartService != null
                ? _chartService.GetMetricDataPoint()
                : new MetricDataPointViewModel();

            vm.Timestamp = point.Timestamp;
            vm.Value = point.Value;

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
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 刷新数据失败", ex);
        }
        finally
        {
            await Task.CompletedTask;
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
        ClearChartCollection(FpsChartPoints);
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
                var exportDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ComputerCompanion");
                Directory.CreateDirectory(exportDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                var allData = new List<MetricDataPoint>();

                allData.AddRange(CpuHistory.Select(point => new MetricDataPoint
                {
                    Timestamp = point.Timestamp,
                    Value = point.Value,
                    Unit = "%",
                    MetricType = MetricType.Gauge
                }));

                allData.AddRange(MemoryHistory.Select(point => new MetricDataPoint
                {
                    Timestamp = point.Timestamp,
                    Value = point.Value,
                    Unit = "%",
                    MetricType = MetricType.Gauge
                }));

                // CSV 导出
                var csvPath = Path.Combine(exportDir, $"performance_data_{timestamp}.csv");
                _dataExportService.ExportToCsv(allData, csvPath);
                _log.Info($"[性能面板] CSV 已导出: {csvPath}");

                // JSON 导出
                var jsonPath = Path.Combine(exportDir, $"performance_data_{timestamp}.json");
                _dataExportService.ExportToJson(allData, jsonPath);
                _log.Info($"[性能面板] JSON 已导出: {jsonPath}");

                // HTML 报告
                var htmlPath = Path.Combine(exportDir, $"performance_report_{timestamp}.html");
                _dataExportService.ExportToHtml(allData, htmlPath);
                _log.Info($"[性能面板] HTML 报告已导出: {htmlPath}");
            }
            else
            {
                _log.Warning("[性能面板] 数据导出服务未初始化");
            }
        }
        catch (Exception ex)
        {
            _log.Error("[性能面板] 导出数据失败", ex);
        }
        finally
        {
            await Task.CompletedTask;
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

        _alertCleanupTimer?.Stop();
        _alertCleanupTimer?.Dispose();

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