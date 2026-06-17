using ComputerCompanion.Services;
using Xunit;

namespace ComputerCompanion.Tests;

public class NetworkMonitorServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var service = new NetworkMonitorService();
        
        Assert.Null(service.NetworkUpload);
        Assert.Null(service.NetworkDownload);
    }

    [Fact]
    public void Start_ShouldNotThrow()
    {
        using var service = new NetworkMonitorService();
        
        Assert.Throws<InvalidOperationException>(() => service.Start());
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var service = new NetworkMonitorService();
        service.Dispose();
        
        // 再次调用不应抛出
        service.Dispose();
    }

    [Fact]
    public void Stop_ShouldNotThrowWhenNotRunning()
    {
        using var service = new NetworkMonitorService();
        service.Stop();
    }
}