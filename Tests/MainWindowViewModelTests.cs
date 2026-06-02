using ComputerCompanion.Models;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using Moq;
using Xunit;

namespace ComputerCompanion.Tests;

public class MainWindowViewModelTests
{
    private readonly Mock<IHardwareMonitorService> _monitorMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Settings _defaultSettings;

    public MainWindowViewModelTests()
    {
        _monitorMock = new Mock<IHardwareMonitorService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _defaultSettings = new Settings();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        _monitorMock.Setup(m => m.HasGpu).Returns(false);
        _monitorMock.Setup(m => m.HasBattery).Returns(false);

        // Act
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Assert
        Assert.Equal("CPU: --", viewModel.CpuInfo);
        Assert.Equal("GPU: --", viewModel.GpuInfo);
        Assert.Equal("内存: --", viewModel.MemoryInfo);
        Assert.Equal("网络: --", viewModel.NetworkInfo);
        Assert.Equal("磁盘: --", viewModel.DiskInfo);
        Assert.Equal("电池: --", viewModel.BatteryInfo);
        Assert.Equal("延迟: --", viewModel.LatencyInfo);
        Assert.False(viewModel.ShowGpu);
        Assert.False(viewModel.ShowBattery);
        Assert.False(viewModel.GameMode);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMonitorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MainWindowViewModel(null!, _defaultSettings, _settingsServiceMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MainWindowViewModel(_monitorMock.Object, null!, _settingsServiceMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsServiceIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MainWindowViewModel(_monitorMock.Object, _defaultSettings, null!));
    }

    [Fact]
    public void OnDataUpdated_UpdatesCpuInfo_WhenShowCpuEnabled()
    {
        // Arrange
        _monitorMock.Setup(m => m.CpuUsage).Returns(50.5f);
        _monitorMock.Setup(m => m.CpuTemp).Returns(65.0f);
        _monitorMock.Setup(m => m.CpuFanSpeed).Returns(2000);
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        _monitorMock.Raise(m => m.DataUpdated += null);

        // Assert
        Assert.Contains("50.5%", viewModel.CpuInfo);
        Assert.Contains("65°C", viewModel.CpuInfo);
        Assert.Contains("2000 RPM", viewModel.CpuInfo);
        Assert.Equal(50.5, viewModel.CpuUsagePercent);
    }

    [Fact]
    public void OnDataUpdated_UpdatesGpuInfo_WhenShowGpuEnabledAndHasGpu()
    {
        // Arrange
        _defaultSettings.ShowGpu = true;
        _monitorMock.Setup(m => m.HasGpu).Returns(true);
        _monitorMock.Setup(m => m.GpuUsage).Returns(75.2f);
        _monitorMock.Setup(m => m.GpuTemp).Returns(72.0f);
        _monitorMock.Setup(m => m.GpuVramUsed).Returns(4.5f);
        _monitorMock.Setup(m => m.GpuVramTotal).Returns(8.0f);
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        _monitorMock.Raise(m => m.DataUpdated += null);

        // Assert
        Assert.Contains("75.2%", viewModel.GpuInfo);
        Assert.Contains("72°C", viewModel.GpuInfo);
        Assert.Contains("4.5/8.0 GB", viewModel.GpuInfo);
        Assert.Equal(75.2, viewModel.GpuUsagePercent);
    }

    [Fact]
    public void OnDataUpdated_UpdatesMemoryInfo_WhenShowMemoryEnabled()
    {
        // Arrange
        _monitorMock.Setup(m => m.MemoryUsed).Returns(8.0f);
        _monitorMock.Setup(m => m.MemoryTotal).Returns(16.0f);
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        _monitorMock.Raise(m => m.DataUpdated += null);

        // Assert
        Assert.Contains("50%", viewModel.MemoryInfo);
        Assert.Contains("8.0/16.0 GB", viewModel.MemoryInfo);
        Assert.Equal(50.0, viewModel.MemoryUsagePercent);
    }

    [Fact]
    public void OnDataUpdated_UpdatesNetworkInfo_WhenShowNetworkEnabled()
    {
        // Arrange
        _monitorMock.Setup(m => m.NetworkDownload).Returns(10.5f);
        _monitorMock.Setup(m => m.NetworkUpload).Returns(2.3f);
        _monitorMock.Setup(m => m.NetworkLatency).Returns(25);
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        _monitorMock.Raise(m => m.DataUpdated += null);

        // Assert
        Assert.Contains("↓ 10.50 MB/s", viewModel.NetworkInfo);
        Assert.Contains("↑ 2.30 MB/s", viewModel.NetworkInfo);
        Assert.Equal("25ms", viewModel.LatencyInfo);
    }

    [Fact]
    public void OnDataUpdated_UpdatesDiskInfo_WhenShowDiskEnabled()
    {
        // Arrange
        _monitorMock.Setup(m => m.DiskFreeSpace).Returns(200.0f);
        _monitorMock.Setup(m => m.DiskTotalSpace).Returns(500.0f);
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        _monitorMock.Raise(m => m.DataUpdated += null);

        // Assert
        Assert.Contains("60%", viewModel.DiskInfo);
        Assert.Contains("300.0/500.0 GB", viewModel.DiskInfo);
        Assert.Equal(60.0, viewModel.DiskUsagePercent);
    }

    [Fact]
    public void OnDataUpdated_UpdatesBatteryInfo_WhenShowBatteryEnabledAndHasBattery()
    {
        // Arrange
        _defaultSettings.ShowBattery = true;
        _monitorMock.Setup(m => m.HasBattery).Returns(true);
        _monitorMock.Setup(m => m.BatteryLevel).Returns(85.0f);
        _monitorMock.Setup(m => m.IsCharging).Returns(true);
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        _monitorMock.Raise(m => m.DataUpdated += null);

        // Assert
        Assert.Contains("⚡", viewModel.BatteryInfo);
        Assert.Contains("85%", viewModel.BatteryInfo);
        Assert.Equal(85.0, viewModel.BatteryLevelPercent);
    }

    [Fact]
    public void ToggleGameMode_TogglesGameModeProperty()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        viewModel.ToggleGameMode();

        // Assert
        Assert.True(viewModel.GameMode);
        Assert.True(_defaultSettings.GameMode);
        _settingsServiceMock.Verify(s => s.SaveSettings(), Times.Once);
        _monitorMock.Verify(m => m.Stop(), Times.Once);
        _monitorMock.Verify(m => m.Start(_defaultSettings.GameModeRefreshInterval), Times.Once);
    }

    [Fact]
    public void ToggleGameMode_TogglesBackToFalse()
    {
        // Arrange
        _defaultSettings.GameMode = true;
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        viewModel.ToggleGameMode();

        // Assert
        Assert.False(viewModel.GameMode);
        Assert.False(_defaultSettings.GameMode);
        _monitorMock.Verify(m => m.Start(_defaultSettings.RefreshInterval), Times.Once);
    }

    [Fact]
    public void UpdateSettings_UpdatesSettingsProperty()
    {
        // Arrange
        var newSettings = new Settings();
        newSettings.GameMode = true;
        
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Act
        viewModel.UpdateSettings(newSettings);

        // Assert
        _settingsServiceMock.Verify(s => s.SaveSettings(), Times.Once);
        _monitorMock.Verify(m => m.Stop(), Times.Once);
        _monitorMock.Verify(m => m.Start(newSettings.GameModeRefreshInterval), Times.Once);
    }

    [Fact]
    public void ShowGpu_IsTrue_WhenMonitorHasGpuAndSettingsShowGpu()
    {
        // Arrange
        _defaultSettings.ShowGpu = true;
        _monitorMock.Setup(m => m.HasGpu).Returns(true);

        // Act
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Assert
        Assert.True(viewModel.ShowGpu);
    }

    [Fact]
    public void ShowBattery_IsTrue_WhenMonitorHasBatteryAndSettingsShowBattery()
    {
        // Arrange
        _defaultSettings.ShowBattery = true;
        _monitorMock.Setup(m => m.HasBattery).Returns(true);

        // Act
        var viewModel = new MainWindowViewModel(
            _monitorMock.Object, 
            _defaultSettings, 
            _settingsServiceMock.Object);

        // Assert
        Assert.True(viewModel.ShowBattery);
    }
}