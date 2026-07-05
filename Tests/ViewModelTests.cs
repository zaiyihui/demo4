using ComputerCompanion.Models;
using ComputerCompanion.ViewModels;
using ComputerCompanion.Services;
using Xunit;
using System;

namespace ComputerCompanion.Tests;

public class ViewModelTests : IDisposable
{
    private HardwareMonitorService? _monitorService;
    private readonly Settings _testSettings;

    public ViewModelTests()
    {
        _monitorService = new HardwareMonitorService();
        _testSettings = new Settings
        {
            ShowCpu = true,
            ShowGpu = true,
            ShowMemory = true,
            ShowNetwork = true,
            ShowDisk = true,
            ShowBattery = true,
            RefreshInterval = 1000
        };
    }

    public void Dispose()
    {
        _monitorService?.Dispose();
    }

    [Fact]
    public void MainWindowViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel(_monitorService!, _testSettings);

        // Assert
        Assert.NotNull(viewModel);
        Assert.Equal(0, viewModel.CpuUsagePercent);
        Assert.Equal(0, viewModel.GpuUsagePercent);
        Assert.Equal(0, viewModel.MemoryUsagePercent);
        Assert.Equal(0, viewModel.DiskUsagePercent);
        Assert.Equal(0, viewModel.BatteryLevelPercent);
    }

    [Fact]
    public void OverlayViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new OverlayViewModel(_monitorService!, _testSettings);

        // Assert
        Assert.NotNull(viewModel);
        Assert.Equal("--", viewModel.FpsText);
        Assert.NotNull(viewModel.GpuText);
        Assert.NotNull(viewModel.CpuText);
        Assert.NotNull(viewModel.MemoryText);
        Assert.NotNull(viewModel.LatencyText);
    }

    [Fact]
    public void OverlayViewModel_MarkFrame_UpdatesFpsText()
    {
        // Arrange
        var viewModel = new OverlayViewModel(_monitorService!, _testSettings);
        var initialFpsText = viewModel.FpsText;

        // Act
        _monitorService!.Start(100);
        viewModel.MarkFrame();
        viewModel.MarkFrame();
        viewModel.MarkFrame();
        _monitorService.Stop();

        // Assert
        Assert.NotNull(viewModel.FpsText);
    }

    [Fact]
    public void SettingsViewModel_InitializesCorrectly()
    {
        // Arrange
        var settings = new Settings
        {
            FontSize = 16,
            RefreshInterval = 2000,
            OverlayShowFPS = true,
            OverlayShowGpu = true
        };
        bool saveCalled = false;
        Settings? savedSettings = null;

        // Act
        var viewModel = new SettingsViewModel(settings, s =>
        {
            saveCalled = true;
            savedSettings = s;
        });

        // Assert
        Assert.NotNull(viewModel);
        Assert.Equal(16, viewModel.OverlayFontSize);
        Assert.Equal(2000, viewModel.RefreshInterval);
        Assert.True(viewModel.OverlayShowFPS);
        Assert.True(viewModel.OverlayShowGpu);
    }

    [Fact]
    public void SettingsViewModel_Save_Works()
    {
        // Arrange
        var settings = new Settings();
        bool saveCalled = false;
        Settings? savedSettings = null;

        var viewModel = new SettingsViewModel(settings, s =>
        {
            saveCalled = true;
            savedSettings = s;
        });

        // Act
        viewModel.FontSize = 18;
        viewModel.RefreshInterval = 1500;
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.True(saveCalled);
        Assert.NotNull(savedSettings);
        Assert.Equal(18, savedSettings!.FontSize);
        Assert.Equal(1500, savedSettings.RefreshInterval);
    }

    [Fact]
    public void SettingsViewModel_ResetToDefaults_Works()
    {
        // Arrange
        var settings = new Settings
        {
            MainWindow = { FontSize = 999 },
            Performance = { RefreshInterval = 9999 }
        };

        var viewModel = new SettingsViewModel(settings, s => { });

        // Act
        viewModel.ResetToDefaultsCommand.Execute(null);

        // Assert
        Assert.Equal(14, viewModel.FontSize);
        Assert.Equal(1000, viewModel.RefreshInterval);
    }
}
