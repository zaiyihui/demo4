using ComputerCompanion.Models;
using ComputerCompanion.ViewModels;
using Xunit;

namespace ComputerCompanion.Tests;

public class SettingsViewModelTests
{
    private Settings _settings;
    private bool _saveCalled;

    public SettingsViewModelTests()
    {
        _settings = new Settings();
        _saveCalled = false;
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(_settings, s => 
        {
            _saveCalled = true;
        });
    }

    [Fact]
    public void Constructor_InitializesFromSettings()
    {
        // Arrange
        _settings.MainWindow.LayoutMode = LayoutMode.Horizontal;
        _settings.MainWindow.TextColor = "#FF0000";
        _settings.MainWindow.BackgroundColor = "#00FF00";
        _settings.MainWindow.BackgroundOpacity = 0.8;
        _settings.MainWindow.FontSize = 16;
        _settings.Performance.RefreshInterval = 2000;
        _settings.Performance.GameMode = true;
        _settings.Performance.GameModeRefreshInterval = 5000;
        _settings.DisplayContent.ShowCpu = false;
        _settings.DisplayContent.ShowGpu = false;
        _settings.DisplayContent.ShowMemory = false;
        _settings.DisplayContent.ShowNetwork = false;
        _settings.DisplayContent.ShowDisk = false;
        _settings.DisplayContent.ShowBattery = false;
        _settings.Overlay.EnableOverlay = false;
        _settings.Overlay.OverlayAlwaysOnTop = false;
        _settings.Overlay.OverlayFontSize = 18;
        _settings.Overlay.OverlayTextColor = "#0000FF";
        _settings.Overlay.OverlayPosition = OverlayPosition.BottomLeft;
        _settings.Overlay.OverlayShowFPS = false;
        _settings.Overlay.OverlayShowGpu = false;
        _settings.Overlay.OverlayShowCpu = false;
        _settings.Overlay.OverlayShowMemory = false;
        _settings.Overlay.OverlayShowLatency = false;
        _settings.Startup.AutoStart = true;
        _settings.Startup.StartMinimized = true;

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(LayoutMode.Horizontal, viewModel.LayoutMode);
        Assert.Equal("#FF0000", viewModel.TextColor);
        Assert.Equal("#00FF00", viewModel.BackgroundColor);
        Assert.Equal(0.8, viewModel.BackgroundOpacity);
        Assert.Equal(16, viewModel.FontSize);
        Assert.Equal(2000, viewModel.RefreshInterval);
        Assert.True(viewModel.GameMode);
        Assert.Equal(5000, viewModel.GameModeRefreshInterval);
        Assert.False(viewModel.ShowCpu);
        Assert.False(viewModel.ShowGpu);
        Assert.False(viewModel.ShowMemory);
        Assert.False(viewModel.ShowNetwork);
        Assert.False(viewModel.ShowDisk);
        Assert.False(viewModel.ShowBattery);
        Assert.False(viewModel.EnableOverlay);
        Assert.False(viewModel.OverlayAlwaysOnTop);
        Assert.Equal(18, viewModel.OverlayFontSize);
        Assert.Equal("#0000FF", viewModel.OverlayTextColor);
        Assert.Equal(OverlayPosition.BottomLeft, viewModel.OverlayPosition);
        Assert.False(viewModel.OverlayShowFPS);
        Assert.False(viewModel.OverlayShowGpu);
        Assert.False(viewModel.OverlayShowCpu);
        Assert.False(viewModel.OverlayShowMemory);
        Assert.False(viewModel.OverlayShowLatency);
        Assert.True(viewModel.AutoStart);
        Assert.True(viewModel.StartMinimized);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(null!, s => { }));
    }

    [Fact]
    public void Save_UpdatesSettings()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LayoutMode = LayoutMode.Horizontal;
        viewModel.TextColor = "#123456";
        viewModel.BackgroundColor = "#654321";
        viewModel.BackgroundOpacity = 0.75;
        viewModel.FontSize = 15;
        viewModel.RefreshInterval = 1500;
        viewModel.GameMode = true;
        viewModel.GameModeRefreshInterval = 4000;
        viewModel.ShowCpu = false;
        viewModel.ShowGpu = true;
        viewModel.ShowMemory = false;
        viewModel.ShowNetwork = true;
        viewModel.ShowDisk = false;
        viewModel.ShowBattery = true;
        viewModel.EnableOverlay = true;
        viewModel.OverlayAlwaysOnTop = false;
        viewModel.OverlayFontSize = 17;
        viewModel.OverlayTextColor = "#ABCDEF";
        viewModel.OverlayPosition = OverlayPosition.TopLeft;
        viewModel.OverlayShowFPS = false;
        viewModel.OverlayShowGpu = true;
        viewModel.OverlayShowCpu = false;
        viewModel.OverlayShowMemory = true;
        viewModel.OverlayShowLatency = false;
        viewModel.AutoStart = false;
        viewModel.StartMinimized = true;

        // Act
        viewModel.Save();

        // Assert
        Assert.True(_saveCalled);
        Assert.Equal(LayoutMode.Horizontal, _settings.MainWindow.LayoutMode);
        Assert.Equal("#123456", _settings.MainWindow.TextColor);
        Assert.Equal("#654321", _settings.MainWindow.BackgroundColor);
        Assert.Equal(0.75, _settings.MainWindow.BackgroundOpacity);
        Assert.Equal(15, _settings.MainWindow.FontSize);
        Assert.Equal(1500, _settings.Performance.RefreshInterval);
        Assert.True(_settings.Performance.GameMode);
        Assert.Equal(4000, _settings.Performance.GameModeRefreshInterval);
        Assert.False(_settings.DisplayContent.ShowCpu);
        Assert.True(_settings.DisplayContent.ShowGpu);
        Assert.False(_settings.DisplayContent.ShowMemory);
        Assert.True(_settings.DisplayContent.ShowNetwork);
        Assert.False(_settings.DisplayContent.ShowDisk);
        Assert.True(_settings.DisplayContent.ShowBattery);
        Assert.True(_settings.Overlay.EnableOverlay);
        Assert.False(_settings.Overlay.OverlayAlwaysOnTop);
        Assert.Equal(17, _settings.Overlay.OverlayFontSize);
        Assert.Equal("#ABCDEF", _settings.Overlay.OverlayTextColor);
        Assert.Equal(OverlayPosition.TopLeft, _settings.Overlay.OverlayPosition);
        Assert.False(_settings.Overlay.OverlayShowFPS);
        Assert.True(_settings.Overlay.OverlayShowGpu);
        Assert.False(_settings.Overlay.OverlayShowCpu);
        Assert.True(_settings.Overlay.OverlayShowMemory);
        Assert.False(_settings.Overlay.OverlayShowLatency);
        Assert.False(_settings.Startup.AutoStart);
        Assert.True(_settings.Startup.StartMinimized);
    }

    [Fact]
    public void ResetToDefaults_ResetsAllSettings()
    {
        // Arrange
        _settings.MainWindow.LayoutMode = LayoutMode.Horizontal;
        _settings.MainWindow.TextColor = "#FF0000";
        _settings.Performance.GameMode = true;
        
        var viewModel = CreateViewModel();
        viewModel.LayoutMode = LayoutMode.Vertical;
        viewModel.TextColor = "#FFFFFF";
        viewModel.GameMode = false;

        // Act
        viewModel.ResetToDefaults();

        // Assert
        Assert.Equal(LayoutMode.Vertical, viewModel.LayoutMode);
        Assert.Equal("#FFFFFF", viewModel.TextColor);
        Assert.False(viewModel.GameMode);
        Assert.Equal(1000, viewModel.RefreshInterval);
        Assert.Equal(3000, viewModel.GameModeRefreshInterval);
        Assert.True(viewModel.ShowCpu);
        Assert.True(viewModel.ShowGpu);
        Assert.True(viewModel.ShowMemory);
        Assert.True(viewModel.ShowNetwork);
        Assert.True(viewModel.ShowDisk);
        Assert.True(viewModel.ShowBattery);
    }

    [Fact]
    public void LayoutMode_PropertyChanged_RaisesEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        bool propertyChanged = false;
        
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.LayoutMode))
                propertyChanged = true;
        };

        // Act
        viewModel.LayoutMode = LayoutMode.Horizontal;

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void TextColor_PropertyChanged_RaisesEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        bool propertyChanged = false;
        
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.TextColor))
                propertyChanged = true;
        };

        // Act
        viewModel.TextColor = "#FF0000";

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void GameMode_PropertyChanged_RaisesEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        bool propertyChanged = false;
        
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.GameMode))
                propertyChanged = true;
        };

        // Act
        viewModel.GameMode = true;

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void SubModuleProperties_AreAccessible()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.NotNull(viewModel.MainWindowSettings);
        Assert.NotNull(viewModel.OverlaySettings);
        Assert.NotNull(viewModel.DisplayContentSettings);
        Assert.NotNull(viewModel.PerformanceSettings);
        Assert.NotNull(viewModel.StartupSettings);
    }
}