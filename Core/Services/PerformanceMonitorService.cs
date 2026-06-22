using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// 性能监控服务 - 实现实时性能监控、告警、历史数据
/// </summary>
public class PerformanceMonitorService : ServiceBase, IPerformanceMonitorService
{
    private readonly Timer _monitorTimer;
    private readonly object _metricsLock = new object();
    private readonly ConcurrentDictionary<string, List<MetricDataPoint>> _historicalMetrics = new();
    private readonly List<AlertRule> _alertRules = new();
    private readonly List<AlertTriggeredEventArgs> _triggeredAlerts = new();

    private PerformanceMetrics _currentMetrics = new();
    private readonly Stopwatch _operationTimer = new();

    private const int MaxHistoryPoints = 1000;
    private const int DefaultIntervalMs = 1000;

    public PerformanceMetrics CurrentMetrics => _currentMetrics;

    public event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;
    public event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;

    public PerformanceMonitorService()
    {
        _monitorTimer = new Timer(
            _ => _ = UpdateMetricsAsync(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);

        // 初始化默认告警规则
        InitializeDefaultAlertRules();
    }

    private void InitializeDefaultAlertRules()
    {
        _alertRules.AddRange(new[]
        {
            new AlertRule
            {
                Name = "HighCpuUsage",
                MetricName = "CpuUsagePercent",
                Operator = ComparisonOperator.GreaterThan,
                Threshold = 90,
                Duration = TimeSpan.FromSeconds(30),
                Severity = AlertSeverity.Warning,
                NotificationChannels = new List<string> { "Log" }
            },
            new AlertRule
            {
                Name = "HighGpuTemperature",
                MetricName = "GpuTemperature",
                Operator = ComparisonOperator.GreaterThan,
                Threshold = 85,
                Duration = TimeSpan.FromSeconds(60),
                Severity = AlertSeverity.Error,
                NotificationChannels = new List<string> { "Log", "Notification" }
            },
            new AlertRule
            {
                Name = "HighMemoryUsage",
                MetricName = "MemoryUsagePercent",
                Operator = ComparisonOperator.GreaterThan,
                Threshold = 90,
                Duration = TimeSpan.FromSeconds(60),
                Severity = AlertSeverity.Warning,
                NotificationChannels = new List<string> { "Log" }
            },
            new AlertRule
            {
                Name = "LowFps",
                MetricName = "Fps",
                Operator = ComparisonOperator.LessThan,
                Threshold = 30,
                Duration = TimeSpan.FromSeconds(10),
                Severity = AlertSeverity.Warning,
                NotificationChannels = new List<string> { "Log" }
            },
            new AlertRule
            {
                Name = "CriticalTemperature",
                MetricName = "CpuTemperature",
                Operator = ComparisonOperator.GreaterThan,
                Threshold = 95,
                Duration = TimeSpan.FromSeconds(10),
                Severity = AlertSeverity.Critical,
                NotificationChannels = new List<string> { "Log", "Notification", "Alert" }
            }
        });
    }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();
        Program.Log("[性能] 性能监控服务已初始化");
        return Task.CompletedTask;
    }

    public override Task StartAsync()
    {
        base.StartAsync();
        _monitorTimer.Change(0, DefaultIntervalMs);
        Program.Log("[性能] 性能监控已启动");
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Program.Log("[性能] 性能监控已停止");
        return base.StopAsync();
    }

    /// <summary>
    /// 更新性能指标
    /// </summary>
    private async Task UpdateMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();

            // 获取CPU使用率
            var cpuUsage = await GetCpuUsageAsync();

            // 获取内存信息
            var memoryUsed = process.WorkingSet64 / (1024.0 * 1024.0);
            var memoryTotal = GetTotalPhysicalMemory() / (1024.0 * 1024.0);

            lock (_metricsLock)
            {
                _currentMetrics = new PerformanceMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    CpuUsagePercent = cpuUsage,
                    MemoryUsedMB = memoryUsed,
                    MemoryTotalMB = memoryTotal,
                    AverageResponseTimeMs = _currentMetrics.AverageResponseTimeMs,
                    P95ResponseTimeMs = _currentMetrics.P95ResponseTimeMs,
                    P99ResponseTimeMs = _currentMetrics.P99ResponseTimeMs,
                    Fps = _currentMetrics.Fps,
                    ErrorsPerMinute = _currentMetrics.ErrorsPerMinute
                };
            }

            // 记录历史数据
            RecordMetric("CpuUsagePercent", cpuUsage, MetricType.Gauge);
            RecordMetric("MemoryUsagePercent", _currentMetrics.MemoryUsagePercent, MetricType.Gauge);

            // 检查告警规则
            var triggeredAlerts = CheckAlertRules();
            if (triggeredAlerts.Count > 0)
            {
                foreach (var alert in triggeredAlerts)
                {
                    AlertTriggered?.Invoke(this, alert);
                }
            }

            // 触发更新事件
            MetricsUpdated?.Invoke(this, new MetricsUpdatedEventArgs
            {
                Metrics = _currentMetrics,
                TriggeredAlerts = triggeredAlerts
            });
        }
        catch (Exception ex)
        {
            Program.Log($"[性能] 更新指标失败: {ex.Message}");
        }
    }

    private async Task<double> GetCpuUsageAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                Thread.Sleep(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0;
            }
        });
    }

    private long GetTotalPhysicalMemory()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt64(obj["TotalPhysicalMemory"]);
            }
        }
        catch { }

        return 16L * 1024 * 1024 * 1024; // 默认16GB
    }

    /// <summary>
    /// 记录自定义指标
    /// </summary>
    public void RecordMetric(string name, double value, MetricType type = MetricType.Gauge)
    {
        var dataPoint = new MetricDataPoint
        {
            Timestamp = DateTime.UtcNow,
            Value = value,
            Tags = new Dictionary<string, string> { ["type"] = type.ToString() }
        };

        if (!_historicalMetrics.ContainsKey(name))
        {
            _historicalMetrics[name] = new List<MetricDataPoint>();
        }

        _historicalMetrics[name].Add(dataPoint);

        // 限制历史数据量
        if (_historicalMetrics[name].Count > MaxHistoryPoints)
        {
            _historicalMetrics[name].RemoveAt(0);
        }
    }

    /// <summary>
    /// 开始计时操作
    /// </summary>
    public IDisposable BeginTiming(string operationName)
    {
        return new OperationTimer(this, operationName);
    }

    internal void EndTiming(string operationName, long elapsedMs)
    {
        RecordMetric($"Operation.{operationName}.Duration", elapsedMs, MetricType.Histogram);

        lock (_metricsLock)
        {
            // 更新响应时间统计
            var values = _historicalMetrics
                .Where(kv => kv.Key.StartsWith("Operation.") && kv.Key.EndsWith(".Duration"))
                .SelectMany(kv => kv.Value)
                .OrderBy(v => v.Value)
                .Select(v => v.Value)
                .ToList();

            if (values.Count > 0)
            {
                _currentMetrics.AverageResponseTimeMs = values.Average();
                _currentMetrics.P95ResponseTimeMs = values.Count > 0 ? values[(int)(values.Count * 0.95)] : 0;
                _currentMetrics.P99ResponseTimeMs = values.Count > 0 ? values[(int)(values.Count * 0.99)] : 0;
            }
        }
    }

    /// <summary>
    /// 获取历史指标
    /// </summary>
    public IEnumerable<MetricDataPoint> GetHistoricalMetrics(string metricName, TimeSpan? duration = null)
    {
        if (!_historicalMetrics.TryGetValue(metricName, out var metrics))
            return Enumerable.Empty<MetricDataPoint>();

        var cutoff = duration.HasValue
            ? DateTime.UtcNow - duration.Value
            : DateTime.MinValue;

        return metrics.Where(m => m.Timestamp >= cutoff).OrderBy(m => m.Timestamp);
    }

    /// <summary>
    /// 添加告警规则
    /// </summary>
    public void AddAlertRule(AlertRule rule)
    {
        if (!_alertRules.Any(r => r.Name == rule.Name))
        {
            _alertRules.Add(rule);
            Program.Log($"[性能] 添加告警规则: {rule.Name}");
        }
    }

    /// <summary>
    /// 移除告警规则
    /// </summary>
    public void RemoveAlertRule(string ruleName)
    {
        var rule = _alertRules.FirstOrDefault(r => r.Name == ruleName);
        if (rule != null)
        {
            _alertRules.Remove(rule);
            Program.Log($"[性能] 移除告警规则: {ruleName}");
        }
    }

    /// <summary>
    /// 检查告警规则
    /// </summary>
    private List<AlertTriggeredEventArgs> CheckAlertRules()
    {
        var triggeredAlerts = new List<AlertTriggeredEventArgs>();

        lock (_metricsLock)
        {
            foreach (var rule in _alertRules.Where(r => r.IsEnabled))
            {
                var currentValue = GetMetricValue(rule.MetricName);
                if (currentValue == null)
                    continue;

                var isViolation = rule.Operator switch
                {
                    ComparisonOperator.GreaterThan => currentValue.Value > rule.Threshold,
                    ComparisonOperator.GreaterThanOrEqual => currentValue.Value >= rule.Threshold,
                    ComparisonOperator.LessThan => currentValue.Value < rule.Threshold,
                    ComparisonOperator.LessThanOrEqual => currentValue.Value <= rule.Threshold,
                    ComparisonOperator.Equal => Math.Abs(currentValue.Value - rule.Threshold) < 0.001,
                    ComparisonOperator.NotEqual => Math.Abs(currentValue.Value - rule.Threshold) >= 0.001,
                    _ => false
                };

                if (isViolation)
                {
                    var alert = new AlertTriggeredEventArgs
                    {
                        Rule = rule,
                        CurrentValue = currentValue.Value,
                        TriggeredAt = DateTime.UtcNow
                    };

                    triggeredAlerts.Add(alert);
                    Program.Log($"[性能] 触发告警: {rule.Name} = {currentValue.Value} (阈值: {rule.Threshold})");
                }
            }
        }

        return triggeredAlerts;
    }

    private double? GetMetricValue(string metricName)
    {
        return metricName switch
        {
            "CpuUsagePercent" => _currentMetrics.CpuUsagePercent,
            "CpuTemperature" => _currentMetrics.CpuTemperature,
            "GpuUsagePercent" => _currentMetrics.GpuUsagePercent,
            "GpuTemperature" => _currentMetrics.GpuTemperature,
            "MemoryUsagePercent" => _currentMetrics.MemoryUsagePercent,
            "MemoryUsedMB" => _currentMetrics.MemoryUsedMB,
            "Fps" => _currentMetrics.Fps,
            "AverageResponseTimeMs" => _currentMetrics.AverageResponseTimeMs,
            "P95ResponseTimeMs" => _currentMetrics.P95ResponseTimeMs,
            "P99ResponseTimeMs" => _currentMetrics.P99ResponseTimeMs,
            _ => null
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitorTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// 操作计时器
/// </summary>
internal class OperationTimer : IDisposable
{
    private readonly PerformanceMonitorService _service;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public OperationTimer(PerformanceMonitorService service, string operationName)
    {
        _service = service;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _service.EndTiming(_operationName, _stopwatch.ElapsedMilliseconds);
    }
}
