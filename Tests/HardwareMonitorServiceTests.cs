using ComputerCompanion.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ComputerCompanion.Tests;

/// <summary>
/// 硬件监控服务单元测试
/// </summary>
public class HardwareMonitorServiceTests : IDisposable
{
    private HardwareMonitorService? _service;

    #region 服务生命周期测试

    [Fact]
    public void Constructor_InitializesService()
    {
        // Arrange & Act
        _service = new HardwareMonitorService();

        // Assert
        Assert.NotNull(_service);
        Assert.Null(_service.CpuUsage);
        Assert.Null(_service.GpuUsage);
        Assert.Null(_service.MemoryUsed);
    }

    [Fact]
    public void Start_WithoutException_InitializesMonitoring()
    {
        // Arrange
        _service = new HardwareMonitorService();

        // Act
        var exception = Record.Exception(() => _service.Start(500));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Stop_AfterStart_StopsMonitoring()
    {
        // Arrange
        _service = new HardwareMonitorService();
        _service.Start(500);

        // Act
        var exception = Record.Exception(() => _service.Stop());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        _service = new HardwareMonitorService();
        _service.Start(500);

        // Act
        var exception = Record.Exception(() => _service.Dispose());

        // Assert
        Assert.Null(exception);
        _service = null; // 确保不会被再次使用
    }

    #endregion

    #region 属性验证测试

    [Fact]
    public void HasGpu_ReturnsFalse_WhenNoGpuData()
    {
        // Arrange
        _service = new HardwareMonitorService();

        // Act
        bool result = _service.HasGpu;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBattery_ReturnsFalse_WhenNoBatteryData()
    {
        // Arrange
        _service = new HardwareMonitorService();

        // Act
        bool result = _service.HasBattery;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 数据更新测试

    [Fact]
    public async Task DataUpdatedEvent_Raised_WhenDataRefreshed()
    {
        // Arrange
        _service = new HardwareMonitorService();
        bool eventRaised = false;
        _service.DataUpdated += () => eventRaised = true;

        // Act
        _service.Start(100);
        await Task.Delay(200); // 等待至少一次数据更新
        _service.Stop();

        // Assert
        Assert.True(eventRaised, "DataUpdated event should be raised when data is refreshed");
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public void Start_Twice_DoesNotThrow()
    {
        // Arrange
        _service = new HardwareMonitorService();
        _service.Start(500);

        // Act
        var exception = Record.Exception(() => _service.Start(500));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        // Arrange
        _service = new HardwareMonitorService();

        // Act
        var exception = Record.Exception(() => _service.Stop());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        // Arrange
        _service = new HardwareMonitorService();
        _service.Start(500);
        _service.Dispose();

        // Act
        var exception = Record.Exception(() => _service.Dispose());

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region IDisposable 实现

    public void Dispose()
    {
        _service?.Dispose();
    }

    #endregion
}
