using ComputerCompanion.Core.Models;
using ComputerCompanion.Core.Services;
using ComputerCompanion.Services;
using Xunit;

namespace ComputerCompanion.Tests.Services;

/// <summary>
/// 配置备份服务单元测试
/// </summary>
public class BackupServiceTests : IDisposable
{
    private readonly string _testBackupDirectory;
    private readonly string _testSettingsPath;
    private readonly BackupService _backupService;
    private readonly MockSettingsService _mockSettingsService;

    public BackupServiceTests()
    {
        _testBackupDirectory = Path.Combine(Path.GetTempPath(), $"backup_test_{Guid.NewGuid():N}");
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid():N}.json");
        _mockSettingsService = new MockSettingsService();
        _backupService = new BackupService(_mockSettingsService, _testBackupDirectory);

        Directory.CreateDirectory(_testBackupDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(_testSettingsPath)!);

        // 创建测试配置文件
        File.WriteAllText(_testSettingsPath, "{\"test\": \"data\"}");
    }

    [Fact]
    public async Task CreateBackupAsync_FullBackup_ReturnsSuccess()
    {
        // Arrange
        await _backupService.InitializeAsync();

        // Act
        var result = await _backupService.CreateBackupAsync(BackupType.Full);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.BackupId);
        Assert.True(result.FilesBackedUp > 0);
        Assert.True(result.SizeBytes > 0);
    }

    [Fact]
    public async Task CreateBackupAsync_DifferentialBackup_ReturnsSuccess()
    {
        // Arrange
        await _backupService.InitializeAsync();

        // Act
        var result = await _backupService.CreateBackupAsync(BackupType.Differential);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(BackupType.Differential, (await _backupService.GetBackupsAsync()).First().Type);
    }

    [Fact]
    public async Task GetBackupsAsync_AfterCreateBackup_ReturnsBackupList()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.CreateBackupAsync(BackupType.Full);

        // Act
        var backups = await _backupService.GetBackupsAsync();

        // Assert
        Assert.NotEmpty(backups);
        Assert.Single(backups);
    }

    [Fact]
    public async Task VerifyBackupIntegrityAsync_ValidBackup_ReturnsTrue()
    {
        // Arrange
        await _backupService.InitializeAsync();
        var createResult = await _backupService.CreateBackupAsync(BackupType.Full);

        // Act
        var isValid = await _backupService.VerifyBackupIntegrityAsync(createResult.BackupId!);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task RestoreBackupAsync_AfterBackup_RestoresSuccessfully()
    {
        // Arrange
        await _backupService.InitializeAsync();
        var createResult = await _backupService.CreateBackupAsync(BackupType.Full);

        // 修改原文件
        await File.WriteAllTextAsync(_testSettingsPath, "{\"modified\": true}");

        // Act
        var restored = await _backupService.RestoreBackupAsync(createResult.BackupId!);

        // Assert
        Assert.True(restored);
    }

    [Fact]
    public async Task DeleteBackupAsync_ExistingBackup_DeletesSuccessfully()
    {
        // Arrange
        await _backupService.InitializeAsync();
        var createResult = await _backupService.CreateBackupAsync(BackupType.Full);
        var initialCount = (await _backupService.GetBackupsAsync()).Count();

        // Act
        await _backupService.DeleteBackupAsync(createResult.BackupId!);
        var finalCount = (await _backupService.GetBackupsAsync()).Count();

        // Assert
        Assert.Equal(initialCount - 1, finalCount);
    }

    public void Dispose()
    {
        _backupService?.Dispose();

        try
        {
            if (Directory.Exists(_testBackupDirectory))
                Directory.Delete(_testBackupDirectory, true);

            if (File.Exists(_testSettingsPath))
                File.Delete(_testSettingsPath);
        }
        catch { }
    }
}

/// <summary>
/// 模拟设置服务
/// </summary>
internal class MockSettingsService : ISettingsService
{
    public Models.Settings GetSettings() => new Models.Settings();
    public void SaveSettings() { }
    public void LoadSettings() { }
    public void UpdateSettingsPath(string newPath) { }
    public void ResetToDefaults() { }
    public ThemeMode LoadThemeMode() => ThemeMode.Dark;
    public void SaveThemeMode(ThemeMode mode) { }
    public List<ComputerCompanion.Services.AlertRule> LoadAlertRules() => new List<ComputerCompanion.Services.AlertRule>();
    public void SaveAlertRules(List<ComputerCompanion.Services.AlertRule> rules) { }
}
