using ComputerCompanion.Services;
using Xunit;

namespace ComputerCompanion.Tests;

public class BatteryMonitorServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var service = new BatteryMonitorService();
        
        Assert.Null(service.BatteryLevel);
        Assert.Null(service.IsCharging);
        Assert.False(service.HasBattery);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var service = new BatteryMonitorService();
        service.Dispose();
        
        service.Dispose();
    }

    [Fact]
    public void Stop_ShouldNotThrowWhenNotRunning()
    {
        using var service = new BatteryMonitorService();
        service.Stop();
    }
}