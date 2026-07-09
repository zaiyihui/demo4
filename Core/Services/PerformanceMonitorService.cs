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

public class PerformanceMonitorService : ServiceBase, IPerformanceMonitorService
{
    private readonly Timer _monitorTimer;
    private readonly ConcurrentDictionary<string, List<MetricDataPoint>> _historicalMetrics = new();
    private readonly List<AlertRule> _alertRules = new();
    private readonly ConcurrentDictionary<string, DateTime> _alertFirstTriggerTimes = new();
    
    private PerformanceMetrics _currentMetrics = new();

    private const int MaxHistoryPoints = 1000;
    private const int DefaultIntervalMs = 1000;

    private double _lastCpuUsage = 0;
    private DateTime _lastCpuUpdate = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime;
    private int _processorCount;

    public PerformanceMetrics CurrentMetrics => Volatile.Read(ref _currentMetrics);

    public event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;
    public event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;

    public PerformanceMonitorService()
    {
        _processorCount = Environment.ProcessorCount;
        
        _monitorTimer = new Timer(
            _ => _ = UpdateMetricsAsync(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);

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

    private async Task UpdateMetricsAsync()
    {
        try
        {
            var metricsData = await Task.Run(() =>
            {
                var process = Process.GetCurrentProcess();
                var cpuUsage = CalculateCpuUsage(process);
                var memoryUsed = process.WorkingSet64 / (1024.0 * 1024.0);
                var memoryTotal = GetTotalPhysicalMemory() / (1024.0 * 1024.0);
                
                return new { CpuUsage = cpuUsage, MemoryUsed = memoryUsed, MemoryTotal = memoryTotal };
            });

            var newMetrics = new PerformanceMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = metricsData.CpuUsage,
                MemoryUsedMB = metricsData.MemoryUsed,
                MemoryTotalMB = metricsData.MemoryTotal,
                AverageResponseTimeMs = _currentMetrics.AverageResponseTimeMs,
                P95ResponseTimeMs = _currentMetrics.P95ResponseTimeMs,
                P99ResponseTimeMs = _currentMetrics.P99ResponseTimeMs,
                Fps = _currentMetrics.Fps,
                ErrorsPerMinute = _currentMetrics.ErrorsPerMinute
            };

            Interlocked.Exchange(ref _currentMetrics, newMetrics);

            RecordMetric("CpuUsagePercent", metricsData.CpuUsage, MetricType.Gauge);
            RecordMetric("MemoryUsagePercent", newMetrics.MemoryUsagePercent, MetricType.Gauge);

            var triggeredAlerts = CheckAlertRules();
            if (triggeredAlerts.Count > 0)
            {
                foreach (var alert in triggeredAlerts)
                {
                    AlertTriggered?.Invoke(this, alert);
                }
            }

            MetricsUpdated?.Invoke(this, new MetricsUpdatedEventArgs
            {
                Metrics = newMetrics,
                TriggeredAlerts = triggeredAlerts
            });
        }
        catch (Exception ex)
        {
            Program.Log($"[性能] 更新指标失败: {ex.Message}");
        }
    }

    private double CalculateCpuUsage(Process process)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var currentTotalProcessorTime = process.TotalProcessorTime;

            var elapsedTime = currentTime - _lastCpuUpdate;
            var elapsedProcessorTime = currentTotalProcessorTime - _lastTotalProcessorTime;

            if (elapsedTime.TotalSeconds > 0.1)
            {
                var cpuUsage = (elapsedProcessorTime.TotalSeconds / elapsedTime.TotalSeconds) / _processorCount * 100;
                _lastCpuUsage = Math.Max(0, Math.Min(100, cpuUsage));
                _lastCpuUpdate = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;
            }

            return _lastCpuUsage;
        }
        catch
        {
            return _lastCpuUsage;
        }
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

        return 16L * 1024 * 1024 * 1024;
    }

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

        var metrics = _historicalMetrics[name];
        lock (metrics)
        {
            metrics.Add(dataPoint);

            while (metrics.Count > MaxHistoryPoints)
            {
                metrics.RemoveAt(0);
            }
        }
    }

    public IDisposable BeginTiming(string operationName)
    {
        return new OperationTimer(this, operationName);
    }

    internal void EndTiming(string operationName, long elapsedMs)
    {
        RecordMetric($"Operation.{operationName}.Duration", elapsedMs, MetricType.Histogram);

        try
        {
            var values = _historicalMetrics
                .Where(kv => kv.Key.StartsWith("Operation.") && kv.Key.EndsWith(".Duration"))
                .SelectMany(kv => kv.Value)
                .OrderBy(v => v.Value)
                .Select(v => v.Value)
                .ToList();

            if (values.Count > 0)
            {
                var currentMetrics = _currentMetrics;
                var newMetrics = new PerformanceMetrics
                {
                    Timestamp = currentMetrics.Timestamp,
                    CpuUsagePercent = currentMetrics.CpuUsagePercent,
                    MemoryUsedMB = currentMetrics.MemoryUsedMB,
                    MemoryTotalMB = currentMetrics.MemoryTotalMB,
                    AverageResponseTimeMs = values.Average(),
                    P95ResponseTimeMs = values[(int)(values.Count * 0.95)],
                    P99ResponseTimeMs = values[(int)(values.Count * 0.99)],
                    Fps = currentMetrics.Fps,
                    ErrorsPerMinute = currentMetrics.ErrorsPerMinute
                };

                Interlocked.Exchange(ref _currentMetrics, newMetrics);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[性能] 更新响应时间统计失败: {ex.Message}");
        }
    }

    public IEnumerable<MetricDataPoint> GetHistoricalMetrics(string metricName, TimeSpan? duration = null)
    {
        if (!_historicalMetrics.TryGetValue(metricName, out var metrics))
            return Enumerable.Empty<MetricDataPoint>();

        var cutoff = duration.HasValue
            ? DateTime.UtcNow - duration.Value
            : DateTime.MinValue;

        lock (metrics)
        {
            return metrics.Where(m => m.Timestamp >= cutoff).OrderBy(m => m.Timestamp).ToList();
        }
    }

    public void AddAlertRule(AlertRule rule)
    {
        if (!_alertRules.Any(r => r.Name == rule.Name))
        {
            _alertRules.Add(rule);
            Program.Log($"[性能] 添加告警规则: {rule.Name}");
        }
    }

    public void RemoveAlertRule(string ruleName)
    {
        var rule = _alertRules.FirstOrDefault(r => r.Name == ruleName);
        if (rule != null)
        {
            _alertRules.Remove(rule);
            Program.Log($"[性能] 移除告警规则: {ruleName}");
        }
    }

    private List<AlertTriggeredEventArgs> CheckAlertRules()
    {
        var triggeredAlerts = new List<AlertTriggeredEventArgs>();
        var currentMetrics = _currentMetrics;

        foreach (var rule in _alertRules.Where(r => r.IsEnabled))
        {
            var currentValue = GetMetricValue(rule.MetricName, currentMetrics);
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
                if (!_alertFirstTriggerTimes.TryGetValue(rule.Name, out var firstTriggerTime))
                {
                    _alertFirstTriggerTimes[rule.Name] = DateTime.UtcNow;
                }
                else
                {
                    var duration = DateTime.UtcNow - firstTriggerTime;
                    if (duration >= rule.Duration)
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
            else
            {
                _alertFirstTriggerTimes.TryRemove(rule.Name, out _);
            }
        }

        return triggeredAlerts;
    }

    private double? GetMetricValue(string metricName, PerformanceMetrics metrics)
    {
        return metricName switch
        {
            "CpuUsagePercent" => metrics.CpuUsagePercent,
            "CpuTemperature" => metrics.CpuTemperature,
            "GpuUsagePercent" => metrics.GpuUsagePercent,
            "GpuTemperature" => metrics.GpuTemperature,
            "MemoryUsagePercent" => metrics.MemoryUsagePercent,
            "MemoryUsedMB" => metrics.MemoryUsedMB,
            "Fps" => metrics.Fps,
            "AverageResponseTimeMs" => metrics.AverageResponseTimeMs,
            "P95ResponseTimeMs" => metrics.P95ResponseTimeMs,
            "P99ResponseTimeMs" => metrics.P99ResponseTimeMs,
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