using ComputerCompanion.Models;
using ComputerCompanion.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ComputerCompanion.Tests;

public class DataStorageServiceTests
{
    private Mock<ISettingsService> CreateMockSettingsService(DataStorageSettings settings)
    {
        var mockSettings = new Settings { DataStorage = settings };
        var mockService = new Mock<ISettingsService>();
        mockService.Setup(s => s.GetSettings()).Returns(mockSettings);
        mockService.Setup(s => s.SaveSettings()).Verifiable();
        return mockService;
    }

    [Fact]
    public void GetDataPath_AppDataLocation_ReturnsAppDataPath()
    {
        var settings = new DataStorageSettings { StorageLocation = DataStorageLocation.AppData };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetDataPath();

        Assert.Contains("ComputerCompanion", result);
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComputerCompanion"), result);
    }

    [Fact]
    public void GetDataPath_InstallationLocation_ReturnsBaseDirectory()
    {
        var settings = new DataStorageSettings { StorageLocation = DataStorageLocation.InstallationDirectory };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetDataPath();

        Assert.Equal(AppContext.BaseDirectory, result);
    }

    [Fact]
    public void GetDataPath_CustomLocation_WithPath_ReturnsCustomPath()
    {
        var customPath = @"D:\CustomData\ComputerCompanion";
        var settings = new DataStorageSettings 
        { 
            StorageLocation = DataStorageLocation.CustomPath,
            CustomPath = customPath
        };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetDataPath();

        Assert.Equal(customPath, result);
    }

    [Fact]
    public void GetDataPath_CustomLocation_EmptyPath_ReturnsAppData()
    {
        var settings = new DataStorageSettings 
        { 
            StorageLocation = DataStorageLocation.CustomPath,
            CustomPath = string.Empty
        };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetDataPath();

        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComputerCompanion"), result);
    }

    [Fact]
    public void ValidatePath_ValidPath_ReturnsTrue()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "TestDataStorage");
        var settings = new DataStorageSettings { StorageLocation = DataStorageLocation.CustomPath, CustomPath = tempPath };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.ValidatePath(tempPath, out string errorMessage);

        Assert.True(result);
        Assert.Empty(errorMessage);
        Assert.True(Directory.Exists(tempPath));

        Directory.Delete(tempPath, true);
    }

    [Fact]
    public void ValidatePath_InvalidPath_ReturnsFalse()
    {
        var invalidPath = @"C:\Windows\System32\config\SAM";
        var settings = new DataStorageSettings { StorageLocation = DataStorageLocation.CustomPath, CustomPath = invalidPath };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.ValidatePath(invalidPath, out string errorMessage);

        Assert.False(result);
        Assert.NotEmpty(errorMessage);
    }

    [Fact]
    public void ValidatePath_EmptyPath_ReturnsFalse()
    {
        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.ValidatePath(string.Empty, out string errorMessage);

        Assert.False(result);
        Assert.Equal("路径不能为空", errorMessage);
    }

    [Fact]
    public void CreateDirectoryIfNotExists_DirectoryDoesNotExist_CreatesDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "TestCreateDir");
        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.CreateDirectoryIfNotExists(tempPath);

        Assert.True(result);
        Assert.True(Directory.Exists(tempPath));

        Directory.Delete(tempPath, true);
    }

    [Fact]
    public void CreateDirectoryIfNotExists_DirectoryExists_ReturnsTrue()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "TestExistingDir");
        Directory.CreateDirectory(tempPath);
        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.CreateDirectoryIfNotExists(tempPath);

        Assert.True(result);

        Directory.Delete(tempPath, true);
    }

    [Fact]
    public void MigrateData_SourceExists_TargetDoesNotExist_MigratesFiles()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), "TestSource");
        var targetPath = Path.Combine(Path.GetTempPath(), "TestTarget");
        
        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "test.txt"), "test content");
        File.WriteAllText(Path.Combine(sourcePath, "config.json"), "{\"test\": true}");

        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.MigrateData(sourcePath, targetPath, out List<string> migratedFiles, out List<string> failedFiles);

        Assert.True(result);
        Assert.Equal(2, migratedFiles.Count);
        Assert.Empty(failedFiles);
        Assert.True(File.Exists(Path.Combine(targetPath, "test.txt")));
        Assert.True(File.Exists(Path.Combine(targetPath, "config.json")));

        Directory.Delete(sourcePath, true);
        Directory.Delete(targetPath, true);
    }

    [Fact]
    public void MigrateData_SourceDoesNotExist_ReturnsTrue()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), "NonExistentSource");
        var targetPath = Path.Combine(Path.GetTempPath(), "TestTarget2");

        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.MigrateData(sourcePath, targetPath, out List<string> migratedFiles, out List<string> failedFiles);

        Assert.True(result);
        Assert.Empty(migratedFiles);
        Assert.Empty(failedFiles);
    }

    [Fact]
    public void TestWriteAccess_ValidPath_ReturnsTrue()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "TestWriteAccess");
        Directory.CreateDirectory(tempPath);

        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.TestWriteAccess(tempPath, out string errorMessage);

        Assert.True(result);
        Assert.Empty(errorMessage);

        Directory.Delete(tempPath, true);
    }

    [Fact]
    public void TestReadAccess_ValidPath_ReturnsTrue()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "TestReadAccess");
        Directory.CreateDirectory(tempPath);
        File.WriteAllText(Path.Combine(tempPath, "test.txt"), "content");

        var settings = new DataStorageSettings();
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        bool result = service.TestReadAccess(tempPath, out string errorMessage);

        Assert.True(result);
        Assert.Empty(errorMessage);

        Directory.Delete(tempPath, true);
    }

    [Fact]
    public void UpdateSettings_ChangesPath_FiresEvent()
    {
        bool eventFired = false;
        string eventPath = string.Empty;

        var initialSettings = new DataStorageSettings { StorageLocation = DataStorageLocation.AppData };
        var mockSettingsService = CreateMockSettingsService(initialSettings);
        var service = new DataStorageService(mockSettingsService.Object);

        service.DataPathChanged += (sender, path) =>
        {
            eventFired = true;
            eventPath = path;
        };

        var newSettings = new DataStorageSettings 
        { 
            StorageLocation = DataStorageLocation.CustomPath,
            CustomPath = @"D:\TestNewPath"
        };
        service.UpdateSettings(newSettings);

        Assert.True(eventFired);
        Assert.Equal(@"D:\TestNewPath", eventPath);
    }

    [Fact]
    public void GetLogPath_ReturnsCorrectPath()
    {
        var customPath = @"D:\CustomData";
        var settings = new DataStorageSettings 
        { 
            StorageLocation = DataStorageLocation.CustomPath,
            CustomPath = customPath
        };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetLogPath();

        Assert.Equal(Path.Combine(customPath, "logs"), result);
    }

    [Fact]
    public void GetCachePath_ReturnsCorrectPath()
    {
        var customPath = @"D:\CustomData";
        var settings = new DataStorageSettings 
        { 
            StorageLocation = DataStorageLocation.CustomPath,
            CustomPath = customPath
        };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetCachePath();

        Assert.Equal(Path.Combine(customPath, "cache"), result);
    }

    [Fact]
    public void GetSettingsPath_ReturnsCorrectPath()
    {
        var customPath = @"D:\CustomData";
        var settings = new DataStorageSettings 
        { 
            StorageLocation = DataStorageLocation.CustomPath,
            CustomPath = customPath
        };
        var mockSettingsService = CreateMockSettingsService(settings);
        var service = new DataStorageService(mockSettingsService.Object);

        var result = service.GetSettingsPath();

        Assert.Equal(Path.Combine(customPath, "settings.json"), result);
    }
}