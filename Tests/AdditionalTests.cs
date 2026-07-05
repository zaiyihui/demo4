using ComputerCompanion.Models;
using Xunit;

namespace ComputerCompanion.Tests;

public class IpcMessageTests
{
    [Fact]
    public void IpcMessage_DefaultValues_AreValid()
    {
        var message = new IpcMessage();
        
        Assert.Equal(string.Empty, message.Type);
        Assert.Equal(string.Empty, message.Data);
    }

    [Fact]
    public void IpcMessage_CanSetProperties()
    {
        var message = new IpcMessage
        {
            Type = "TestType",
            Data = "TestData"
        };
        
        Assert.Equal("TestType", message.Type);
        Assert.Equal("TestData", message.Data);
    }

    [Fact]
    public void IpcMessageTypes_ContainsExpectedValues()
    {
        Assert.Equal("SettingsChanged", IpcMessageTypes.SettingsChanged);
        Assert.Equal("ShowMainWindow", IpcMessageTypes.ShowMainWindow);
        Assert.Equal("ExitApplication", IpcMessageTypes.ExitApplication);
        Assert.Equal("OverlayReady", IpcMessageTypes.OverlayReady);
    }
}

public class SettingsModelTests
{
    [Fact]
    public void Settings_DefaultValues_AreCorrect()
    {
        var settings = new Settings();
        
        Assert.Equal(LayoutMode.Vertical, settings.MainWindow.LayoutMode);
        Assert.Equal("#FFFFFF", settings.MainWindow.TextColor);
        Assert.Equal("#1a1a2eea", settings.MainWindow.BackgroundColor);
        Assert.Equal(0.9, settings.MainWindow.BackgroundOpacity);
        Assert.Equal(14, settings.MainWindow.FontSize);
        Assert.Equal(1000, settings.Performance.RefreshInterval);
        Assert.True(settings.Overlay.EnableOverlay);
        Assert.True(settings.Overlay.OverlayAlwaysOnTop);
        Assert.Equal(OverlayPosition.TopRight, settings.Overlay.OverlayPosition);
    }

    [Fact]
    public void Settings_CanModifyAllProperties()
    {
        var settings = new Settings
        {
            MainWindow = { 
                LayoutMode = LayoutMode.Horizontal,
                TextColor = "#FF0000",
                BackgroundColor = "#00FF00",
                BackgroundOpacity = 0.5,
                FontSize = 20
            },
            Performance = { RefreshInterval = 2000 },
            Overlay = { 
                EnableOverlay = false,
                OverlayAlwaysOnTop = false,
                OverlayPosition = OverlayPosition.BottomLeft
            }
        };
        
        Assert.Equal(LayoutMode.Horizontal, settings.MainWindow.LayoutMode);
        Assert.Equal("#FF0000", settings.MainWindow.TextColor);
        Assert.Equal("#00FF00", settings.MainWindow.BackgroundColor);
        Assert.Equal(0.5, settings.MainWindow.BackgroundOpacity);
        Assert.Equal(20, settings.MainWindow.FontSize);
        Assert.Equal(2000, settings.Performance.RefreshInterval);
        Assert.False(settings.Overlay.EnableOverlay);
        Assert.False(settings.Overlay.OverlayAlwaysOnTop);
        Assert.Equal(OverlayPosition.BottomLeft, settings.Overlay.OverlayPosition);
    }

    [Fact]
    public void LayoutMode_Enum_HasExpectedValues()
    {
        Assert.Equal(0, (int)LayoutMode.Vertical);
        Assert.Equal(1, (int)LayoutMode.Horizontal);
    }

    [Fact]
    public void OverlayPosition_Enum_HasExpectedValues()
    {
        Assert.Equal(0, (int)OverlayPosition.TopLeft);
        Assert.Equal(1, (int)OverlayPosition.TopRight);
        Assert.Equal(2, (int)OverlayPosition.BottomLeft);
        Assert.Equal(3, (int)OverlayPosition.BottomRight);
    }
}
