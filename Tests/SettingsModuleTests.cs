using ComputerCompanion.Models;
using Xunit;
using System;

namespace ComputerCompanion.Tests;

public class SettingsModuleTests
{
    [Fact]
    public void Settings_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var settings = new Settings();

        // Assert
        Assert.NotNull(settings.MainWindow);
        Assert.NotNull(settings.Overlay);
        Assert.NotNull(settings.DisplayContent);
        Assert.NotNull(settings.Performance);
        Assert.NotNull(settings.Startup);
    }

    [Fact]
    public void Settings_ResetToDefaults_Works()
    {
        // Arrange
        var settings = new Settings();
        settings.MainWindow.FontSize = 999;
        settings.Overlay.EnableOverlay = false;
        settings.Performance.RefreshInterval = 9999;

        // Act
        settings.ResetToDefaults();

        // Assert
        Assert.Equal(14, settings.MainWindow.FontSize);
        Assert.True(settings.Overlay.EnableOverlay);
        Assert.Equal(1000, settings.Performance.RefreshInterval);
    }

    #region MainWindowSettings Tests

    [Fact]
    public void MainWindowSettings_InitializesWithDefaults()
    {
        // Arrange & Act
        var settings = new MainWindowSettings();

        // Assert
        Assert.Equal(LayoutMode.Vertical, settings.LayoutMode);
        Assert.Equal("#FFFFFF", settings.TextColor);
        Assert.Equal("#1a1a2eea", settings.BackgroundColor);
        Assert.Equal(0.9, settings.BackgroundOpacity);
        Assert.Equal(14, settings.FontSize);
        Assert.Equal(100, settings.WindowX);
        Assert.Equal(100, settings.WindowY);
    }

    [Fact]
    public void MainWindowSettings_CanSetCustomValues()
    {
        // Arrange
        var settings = new MainWindowSettings();

        // Act
        settings.LayoutMode = LayoutMode.Horizontal;
        settings.TextColor = "#FF0000";
        settings.FontSize = 20;

        // Assert
        Assert.Equal(LayoutMode.Horizontal, settings.LayoutMode);
        Assert.Equal("#FF0000", settings.TextColor);
        Assert.Equal(20, settings.FontSize);
    }

    #endregion

    #region OverlaySettings Tests

    [Fact]
    public void OverlaySettings_InitializesWithDefaults()
    {
        // Arrange & Act
        var settings = new OverlaySettings();

        // Assert
        Assert.True(settings.EnableOverlay);
        Assert.True(settings.OverlayAlwaysOnTop);
        Assert.Equal(16, settings.OverlayFontSize);
        Assert.Equal("#76B900", settings.OverlayTextColor);
        Assert.Equal(OverlayPosition.TopRight, settings.OverlayPosition);
        Assert.True(settings.OverlayShowFPS);
        Assert.True(settings.OverlayShowGpu);
        Assert.True(settings.OverlayShowCpu);
        Assert.True(settings.OverlayShowMemory);
        Assert.True(settings.OverlayShowLatency);
    }

    [Fact]
    public void OverlaySettings_CanSetCustomValues()
    {
        // Arrange
        var settings = new OverlaySettings();

        // Act
        settings.EnableOverlay = false;
        settings.OverlayTextColor = "#00FFFF";
        settings.OverlayPosition = OverlayPosition.BottomLeft;
        settings.OverlayShowFPS = false;

        // Assert
        Assert.False(settings.EnableOverlay);
        Assert.Equal("#00FFFF", settings.OverlayTextColor);
        Assert.Equal(OverlayPosition.BottomLeft, settings.OverlayPosition);
        Assert.False(settings.OverlayShowFPS);
    }

    #endregion

    #region DisplayContentSettings Tests

    [Fact]
    public void DisplayContentSettings_InitializesWithDefaults()
    {
        // Arrange & Act
        var settings = new DisplayContentSettings();

        // Assert
        Assert.True(settings.ShowCpu);
        Assert.True(settings.ShowGpu);
        Assert.True(settings.ShowMemory);
        Assert.True(settings.ShowNetwork);
        Assert.True(settings.ShowDisk);
        Assert.True(settings.ShowBattery);
    }

    [Fact]
    public void DisplayContentSettings_CanToggleContent()
    {
        // Arrange
        var settings = new DisplayContentSettings();

        // Act
        settings.ShowCpu = false;
        settings.ShowBattery = false;

        // Assert
        Assert.False(settings.ShowCpu);
        Assert.False(settings.ShowBattery);
        Assert.True(settings.ShowGpu);
    }

    #endregion

    #region PerformanceSettings Tests

    [Fact]
    public void PerformanceSettings_InitializesWithDefaults()
    {
        // Arrange & Act
        var settings = new PerformanceSettings();

        // Assert
        Assert.Equal(1000, settings.RefreshInterval);
        Assert.False(settings.GameMode);
        Assert.Equal(3000, settings.GameModeRefreshInterval);
    }

    [Fact]
    public void PerformanceSettings_CanConfigureGameMode()
    {
        // Arrange
        var settings = new PerformanceSettings();

        // Act
        settings.GameMode = true;
        settings.GameModeRefreshInterval = 5000;

        // Assert
        Assert.True(settings.GameMode);
        Assert.Equal(5000, settings.GameModeRefreshInterval);
    }

    #endregion

    #region StartupSettings Tests

    [Fact]
    public void StartupSettings_InitializesWithDefaults()
    {
        // Arrange & Act
        var settings = new StartupSettings();

        // Assert
        Assert.False(settings.AutoStart);
        Assert.False(settings.StartMinimized);
    }

    [Fact]
    public void StartupSettings_CanConfigureStartup()
    {
        // Arrange
        var settings = new StartupSettings();

        // Act
        settings.AutoStart = true;
        settings.StartMinimized = true;

        // Assert
        Assert.True(settings.AutoStart);
        Assert.True(settings.StartMinimized);
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void Settings_BackwardCompatibility_MainWindow()
    {
        // Arrange
        var settings = new Settings();

        // Act - 使用废弃的属性
        settings.LayoutMode = LayoutMode.Horizontal;
        settings.TextColor = "#ABCDEF";
        settings.BackgroundOpacity = 0.8;

        // Assert - 通过子模块访问
        Assert.Equal(LayoutMode.Horizontal, settings.MainWindow.LayoutMode);
        Assert.Equal("#ABCDEF", settings.MainWindow.TextColor);
        Assert.Equal(0.8, settings.MainWindow.BackgroundOpacity);
    }

    [Fact]
    public void Settings_BackwardCompatibility_Overlay()
    {
        // Arrange
        var settings = new Settings();

        // Act - 使用废弃的属性
        settings.EnableOverlay = false;
        settings.OverlayTextColor = "#123456";
        settings.OverlayShowFPS = false;

        // Assert
        Assert.False(settings.Overlay.EnableOverlay);
        Assert.Equal("#123456", settings.Overlay.OverlayTextColor);
        Assert.False(settings.Overlay.OverlayShowFPS);
    }

    [Fact]
    public void Settings_BackwardCompatibility_DisplayContent()
    {
        // Arrange
        var settings = new Settings();

        // Act - 使用废弃的属性
        settings.ShowCpu = false;
        settings.ShowGpu = false;

        // Assert
        Assert.False(settings.DisplayContent.ShowCpu);
        Assert.False(settings.DisplayContent.ShowGpu);
    }

    [Fact]
    public void Settings_BackwardCompatibility_Performance()
    {
        // Arrange
        var settings = new Settings();

        // Act - 使用废弃的属性
        settings.RefreshInterval = 2000;
        settings.GameMode = true;
        settings.GameModeRefreshInterval = 4000;

        // Assert
        Assert.Equal(2000, settings.Performance.RefreshInterval);
        Assert.True(settings.Performance.GameMode);
        Assert.Equal(4000, settings.Performance.GameModeRefreshInterval);
    }

    [Fact]
    public void Settings_BackwardCompatibility_Startup()
    {
        // Arrange
        var settings = new Settings();

        // Act - 使用废弃的属性
        settings.AutoStart = true;
        settings.StartMinimized = true;

        // Assert
        Assert.True(settings.Startup.AutoStart);
        Assert.True(settings.Startup.StartMinimized);
    }

    #endregion

    #region LayoutMode Tests

    [Fact]
    public void LayoutMode_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)LayoutMode.Vertical);
        Assert.Equal(1, (int)LayoutMode.Horizontal);
    }

    #endregion

    #region OverlayPosition Tests

    [Fact]
    public void OverlayPosition_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)OverlayPosition.TopLeft);
        Assert.Equal(1, (int)OverlayPosition.TopRight);
        Assert.Equal(2, (int)OverlayPosition.BottomLeft);
        Assert.Equal(3, (int)OverlayPosition.BottomRight);
    }

    #endregion
}
