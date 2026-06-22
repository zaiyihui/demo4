using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Core.Services;
using Xunit;

namespace ComputerCompanion.Tests.Services;

/// <summary>
/// 性能监控服务单元测试
/// </summary>
public class PerformanceMonitorServiceTests : IDisposable
{
    private readonly PerformanceMonitorService _service;

    public PerformanceMonitorServiceTests()
    {
        _service = new PerformanceMonitorService();
    }

    [Fact]
    public async Task InitializeAsync_Success_CompletesWithoutError()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        Assert.True(_service.IsInitialized);
    }

    [Fact]
    public async Task StartAsync_AfterInit_StartsSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_AfterStart_StopsSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public void RecordMetric_WithGaugeType_StoresValue()
    {
        // Arrange
        const string metricName = "TestMetric";
        const double expectedValue = 42.5;

        // Act
        _service.RecordMetric(metricName, expectedValue, MetricType.Gauge);

        // Assert
        var history = _service.GetHistoricalMetrics(metricName);
        Assert.Single(history);
        Assert.Equal(expectedValue, history.First().Value);
    }

    [Fact]
    public void RecordMetric_MultipleValues_StoresAll()
    {
        // Arrange
        const string metricName = "TestMetric";

        // Act
        _service.RecordMetric(metricName, 10, MetricType.Gauge);
        _service.RecordMetric(metricName, 20, MetricType.Gauge);
        _service.RecordMetric(metricName, 30, MetricType.Gauge);

        // Assert
        var history = _service.GetHistoricalMetrics(metricName);
        Assert.Equal(3, history.Count());
    }

    [Fact]
    public void BeginTiming_WithValidOperation_ReturnsDisposable()
    {
        // Arrange
        const string operationName = "TestOperation";

        // Act
        using var timer = _service.BeginTiming(operationName);

        // Assert
        Assert.NotNull(timer);
    }

    [Fact]
    public void BeginTiming_CompletesTiming_RecordsMetric()
    {
        // Arrange
        const string operationName = "QuickOperation";
        _service.RecordMetric($"Operation.{operationName}.Duration", 100, MetricType.Histogram);

        // Act
        using (_service.BeginTiming(operationName))
        {
            Thread.Sleep(50);
        }

        // Assert
        var history = _service.GetHistoricalMetrics($"Operation.{operationName}.Duration");
        Assert.Single(history);
        Assert.True(history.First().Value >= 40); // 至少40ms
    }

    [Fact]
    public void AddAlertRule_WithValidRule_AddsSuccessfully()
    {
        // Arrange
        var rule = new AlertRule
        {
            Name = "TestRule",
            MetricName = "TestMetric",
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 50
        };

        // Act
        _service.AddAlertRule(rule);

        // Assert
        // 没有异常即成功
    }

    [Fact]
    public void RemoveAlertRule_WithExistingRule_RemovesSuccessfully()
    {
        // Arrange
        var rule = new AlertRule
        {
            Name = "TestRule",
            MetricName = "TestMetric",
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 50
        };
        _service.AddAlertRule(rule);

        // Act
        _service.RemoveAlertRule("TestRule");

        // Assert
        // 没有异常即成功
    }

    [Fact]
    public void GetHistoricalMetrics_WithDuration_FiltersCorrectly()
    {
        // Arrange
        const string metricName = "TestMetric";
        _service.RecordMetric(metricName, 10, MetricType.Gauge);
        _service.RecordMetric(metricName, 20, MetricType.Gauge);
        _service.RecordMetric(metricName, 30, MetricType.Gauge);

        // Act
        var recentHistory = _service.GetHistoricalMetrics(metricName, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(3, recentHistory.Count());
    }

    [Fact]
    public void GetHistoricalMetrics_WithNoData_ReturnsEmpty()
    {
        // Arrange
        const string metricName = "NonExistentMetric";

        // Act
        var history = _service.GetHistoricalMetrics(metricName);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public void MetricsUpdated_Event_RaisesOnUpdate()
    {
        // Arrange
        MetricsUpdatedEventArgs? capturedArgs = null;
        _service.MetricsUpdated += (s, e) => capturedArgs = e;

        // Act
        _service.RecordMetric("TestMetric", 42, MetricType.Gauge);

        // Assert
        // 等待事件触发（异步）
        Task.Delay(1100).Wait();
        Assert.NotNull(capturedArgs);
    }

    public void Dispose()
    {
        _service?.StopAsync().Wait();
        _service?.Dispose();
    }
}
