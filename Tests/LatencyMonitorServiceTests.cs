using ComputerCompanion.Services;
using Xunit;

namespace ComputerCompanion.Tests;

public class LatencyMonitorServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var service = new LatencyMonitorService();
        
        Assert.Null(service.NetworkLatency);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var service = new LatencyMonitorService();
        service.Dispose();
        
        service.Dispose();
    }

    [Fact]
    public void Stop_ShouldNotThrowWhenNotRunning()
    {
        using var service = new LatencyMonitorService();
        service.Stop();
    }
}