using ComputerCompanion.Models;
using ComputerCompanion.Services;
using System;
using System.IO;
using Xunit;

namespace ComputerCompanion.Tests;

public class SettingsServiceTests : IDisposable
{
    private SettingsService? _service;
    private string _testSettingsPath = string.Empty;

    public void Dispose()
    {
        CleanupTestFiles();
    }

    private void CleanupTestFiles()
    {
        if (!string.IsNullOrEmpty(_testSettingsPath) && File.Exists(_testSettingsPath))
        {
            try
            {
                File.Delete(_testSettingsPath);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Constructor_CreatesSettingsFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
        _testSettingsPath = tempPath;
        
        CleanupTestFiles();
        
        var originalPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var testFolder = Path.Combine(originalPath, "ComputerCompanionTest");
        
        try
        {
            Directory.CreateDirectory(testFolder);
            var testSettingsPath = Path.Combine(testFolder, "settings.json");
            _testSettingsPath = testSettingsPath;
            
            var service = new TestableSettingsService(testSettingsPath);
            
            Assert.NotNull(service.GetSettings());
            Assert.True(File.Exists(testSettingsPath));
        }
        finally
        {
            try
            {
                if (Directory.Exists(testFolder))
                    Directory.Delete(testFolder, true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void GetSettings_ReturnsValidSettings()
    {
        _service = new SettingsService();
        var settings = _service.GetSettings();
        
        Assert.NotNull(settings);
        Assert.IsType<Settings>(settings);
    }

    [Fact]
    public void SaveSettings_PreservesValues()
    {
        _service = new SettingsService();
        var settings = _service.GetSettings();
        
        settings.ShowCpu = false;
        settings.ShowGpu = false;
        settings.FontSize = 20;
        
        _service.SaveSettings();
        _service.LoadSettings();
        
        var loadedSettings = _service.GetSettings();
        
        Assert.False(loadedSettings.ShowCpu);
        Assert.False(loadedSettings.ShowGpu);
        Assert.Equal(20, loadedSettings.FontSize);
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaultValues()
    {
        _service = new SettingsService();
        var settings = _service.GetSettings();
        
        settings.ShowCpu = false;
        settings.ShowGpu = false;
        settings.FontSize = 999;
        _service.SaveSettings();
        
        _service.ResetToDefaults();
        
        var resetSettings = _service.GetSettings();
        
        Assert.True(resetSettings.ShowCpu);
        Assert.True(resetSettings.ShowGpu);
        Assert.Equal(14, resetSettings.FontSize);
    }

    private class TestableSettingsService : SettingsService
    {
        private readonly string _testPath;
        
        public TestableSettingsService(string testPath)
        {
            _testPath = testPath;
        }
    }
}

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
        
        Assert.Equal(LayoutMode.Vertical, settings.LayoutMode);
        Assert.Equal("#FFFFFF", settings.TextColor);
        Assert.Equal("#1a1a2eea", settings.BackgroundColor);
        Assert.Equal(0.9, settings.BackgroundOpacity);
        Assert.Equal(14, settings.FontSize);
        Assert.Equal(1000, settings.RefreshInterval);
        Assert.False(settings.GameMode);
        Assert.True(settings.EnableOverlay);
        Assert.True(settings.OverlayAlwaysOnTop);
        Assert.Equal(OverlayPosition.TopRight, settings.OverlayPosition);
    }

    [Fact]
    public void Settings_CanModifyAllProperties()
    {
        var settings = new Settings
        {
            LayoutMode = LayoutMode.Horizontal,
            TextColor = "#FF0000",
            BackgroundColor = "#00FF00",
            BackgroundOpacity = 0.5,
            FontSize = 20,
            RefreshInterval = 2000,
            GameMode = true,
            EnableOverlay = false,
            OverlayAlwaysOnTop = false,
            OverlayPosition = OverlayPosition.BottomLeft
        };
        
        Assert.Equal(LayoutMode.Horizontal, settings.LayoutMode);
        Assert.Equal("#FF0000", settings.TextColor);
        Assert.Equal("#00FF00", settings.BackgroundColor);
        Assert.Equal(0.5, settings.BackgroundOpacity);
        Assert.Equal(20, settings.FontSize);
        Assert.Equal(2000, settings.RefreshInterval);
        Assert.True(settings.GameMode);
        Assert.False(settings.EnableOverlay);
        Assert.False(settings.OverlayAlwaysOnTop);
        Assert.Equal(OverlayPosition.BottomLeft, settings.OverlayPosition);
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
