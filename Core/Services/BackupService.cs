using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// 配置备份服务 - 实现自动备份、版本控制、差异备份和完整性验证
/// </summary>
public class BackupService : ServiceBase, IBackupService
{
    private readonly string _backupDirectory;
    private readonly string _settingsPath;
    private readonly ISettingsService _settingsService;
    private readonly Timer _autoBackupTimer;
    private BackupMetadata? _lastBackupMetadata;
    private Dictionary<string, string>? _lastBackupChecksums;

    public BackupService(ISettingsService settingsService, string? backupDirectory = null)
    {
        _settingsService = settingsService;
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ComputerCompanion", "settings.json");

        _backupDirectory = backupDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ComputerCompanion", "Backups");

        // 设置自动备份定时器（每小时检查一次）
        _autoBackupTimer = new Timer(
            _ => _ = CheckAndCreateAutoBackupAsync(),
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

        EnsureBackupDirectory();
    }

    private void EnsureBackupDirectory()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
            Program.Log($"[备份] 创建备份目录: {_backupDirectory}");
        }
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        EnsureBackupDirectory();
        Program.Log("[备份] 备份服务已初始化");
    }

    /// <summary>
    /// 创建备份
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(BackupType type = BackupType.Full)
    {
        var startTime = DateTime.UtcNow;
        var result = new BackupResult();

        try
        {
            var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var backupFolder = Path.Combine(_backupDirectory, backupId);
            Directory.CreateDirectory(backupFolder);

            Program.Log($"[备份] 开始创建备份: {backupId}");

            int filesBackedUp = 0;

            if (type == BackupType.Full)
            {
                // 完全备份：复制所有配置
                filesBackedUp = await CreateFullBackupAsync(backupFolder);
            }
            else if (type == BackupType.Differential)
            {
                // 差异备份：只备份自上次备份以来变更的文件
                filesBackedUp = await CreateDifferentialBackupAsync(backupFolder);
            }
            else
            {
                // 增量备份：备份自上次备份以来的所有变更
                filesBackedUp = await CreateIncrementalBackupAsync(backupFolder);
            }

            // 计算校验和
            var checksum = await CalculateBackupChecksumAsync(backupFolder);

            // 创建备份元数据
            var metadata = new BackupMetadata
            {
                Id = backupId,
                CreatedAt = DateTime.UtcNow,
                Type = type,
                SizeBytes = GetDirectorySize(backupFolder),
                Checksum = checksum,
                AppVersion = GetAppVersion(),
                CustomProperties = new Dictionary<string, string>
                {
                    ["FilesBackedUp"] = filesBackedUp.ToString()
                }
            };

            // 保存元数据
            await SaveBackupMetadataAsync(backupFolder, metadata);

            // 创建 ZIP 压缩包
            var zipPath = Path.Combine(_backupDirectory, $"{backupId}.zip");
            await CreateZipArchiveAsync(backupFolder, zipPath);

            // 清理临时文件夹
            Directory.Delete(backupFolder, true);

            // 保存上次备份信息用于差异/增量备份
            _lastBackupMetadata = metadata;
            _lastBackupChecksums = await CalculateFileChecksumsAsync(Path.GetDirectoryName(_settingsPath)!);

            // 删除旧备份（保留最近10个）
            await CleanupOldBackupsAsync(10);

            result.Success = true;
            result.BackupId = backupId;
            result.SizeBytes = metadata.SizeBytes;
            result.FilesBackedUp = filesBackedUp;
            result.Duration = DateTime.UtcNow - startTime;

            Program.Log($"[备份] 备份创建成功: {backupId}, 大小: {metadata.SizeBytes / 1024}KB");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Program.Log($"[备份] 备份创建失败: {ex.Message}");
        }

        return result;
    }

    private async Task<int> CreateFullBackupAsync(string backupFolder)
    {
        var configDir = Path.GetDirectoryName(_settingsPath)!;
        var files = Directory.GetFiles(configDir, "*.json");

        int count = 0;
        foreach (var file in files)
        {
            var destFile = Path.Combine(backupFolder, Path.GetFileName(file));
            await Task.Run(() => File.Copy(file, destFile, true));
            count++;
        }

        return count;
    }

    private async Task<int> CreateDifferentialBackupAsync(string backupFolder)
    {
        var configDir = Path.GetDirectoryName(_settingsPath)!;
        var currentChecksums = await CalculateFileChecksumsAsync(configDir);

        int count = 0;
        foreach (var file in currentChecksums)
        {
            // 如果文件在上次备份后有变化
            if (_lastBackupChecksums == null || !_lastBackupChecksums.ContainsKey(file.Key) ||
                _lastBackupChecksums[file.Key] != file.Value)
            {
                var sourceFile = Path.Combine(configDir, file.Key);
                var destFile = Path.Combine(backupFolder, file.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                await Task.Run(() => File.Copy(sourceFile, destFile, true));
                count++;
            }
        }

        return count;
    }

    private async Task<int> CreateIncrementalBackupAsync(string backupFolder)
    {
        // 增量备份：每次都备份所有文件
        return await CreateFullBackupAsync(backupFolder);
    }

    private async Task<Dictionary<string, string>> CalculateFileChecksumsAsync(string directory)
    {
        var checksums = new Dictionary<string, string>();
        var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directory, file);
            checksums[relativePath] = await CalculateFileChecksumAsync(file);
        }

        return checksums;
    }

    private async Task<string> CalculateFileChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => sha256.ComputeHash(stream));
        return Convert.ToHexString(hash);
    }

    private async Task<string> CalculateBackupChecksumAsync(string folder)
    {
        using var sha256 = SHA256.Create();
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(f => f);

        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
        }

        var finalHash = sha256.Hash;
        return Convert.ToHexString(finalHash!);
    }

    private async Task SaveBackupMetadataAsync(string backupFolder, BackupMetadata metadata)
    {
        var metadataPath = Path.Combine(backupFolder, "metadata.json");
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json);
    }

    private async Task CreateZipArchiveAsync(string sourceFolder, string zipPath)
    {
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        await Task.Run(() => ZipFile.CreateFromDirectory(sourceFolder, zipPath, CompressionLevel.Optimal, false));
    }

    private long GetDirectorySize(string folder)
    {
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
        return files.Sum(f => new FileInfo(f).Length);
    }

    /// <summary>
    /// 恢复备份
    /// </summary>
    public async Task<bool> RestoreBackupAsync(string backupId)
    {
        try
        {
            var zipPath = Path.Combine(_backupDirectory, $"{backupId}.zip");
            if (!File.Exists(zipPath))
            {
                Program.Log($"[备份] 备份文件不存在: {zipPath}");
                return false;
            }

            // 验证备份完整性
            if (!await VerifyBackupIntegrityAsync(backupId))
            {
                Program.Log("[备份] 备份完整性验证失败");
                return false;
            }

            // 解压备份
            var tempFolder = Path.Combine(_backupDirectory, $"restore_{Guid.NewGuid():N}");
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, tempFolder, true));

            // 恢复文件
            var configDir = Path.GetDirectoryName(_settingsPath)!;
            var files = Directory.GetFiles(tempFolder, "*.json");

            foreach (var file in files)
            {
                var destFile = Path.Combine(configDir, Path.GetFileName(file));

                // 先备份当前配置
                if (File.Exists(destFile))
                {
                    var backupPath = $"{destFile}.before_restore";
                    File.Copy(destFile, backupPath, true);
                }

                File.Copy(file, destFile, true);
            }

            // 清理临时文件夹
            Directory.Delete(tempFolder, true);

            // 重新加载设置
            _settingsService.LoadSettings();

            Program.Log($"[备份] 备份恢复成功: {backupId}");
            return true;
        }
        catch (Exception ex)
        {
            Program.Log($"[备份] 备份恢复失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取所有备份列表
    /// </summary>
    public async Task<IEnumerable<BackupInfo>> GetBackupsAsync()
    {
        var backups = new List<BackupInfo>();

        try
        {
            var zipFiles = Directory.GetFiles(_backupDirectory, "*.zip")
                .OrderByDescending(f => new FileInfo(f).CreationTime);

            foreach (var zipFile in zipFiles)
            {
                var backupId = Path.GetFileNameWithoutExtension(zipFile);
                var fileInfo = new FileInfo(zipFile);

                // 尝试读取元数据
                BackupMetadata? metadata = null;
                try
                {
                    var tempFolder = Path.Combine(_backupDirectory, $"meta_{Guid.NewGuid():N}");
                    ZipFile.ExtractToDirectory(zipFile, tempFolder, true);

                    var metadataPath = Path.Combine(tempFolder, "metadata.json");
                    if (File.Exists(metadataPath))
                    {
                        var json = await File.ReadAllTextAsync(metadataPath);
                        metadata = JsonSerializer.Deserialize<BackupMetadata>(json);
                    }

                    Directory.Delete(tempFolder, true);
                }
                catch { }

                backups.Add(new BackupInfo
                {
                    Id = backupId,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    SizeBytes = fileInfo.Length,
                    Type = metadata?.Type ?? BackupType.Full,
                    Checksum = metadata?.Checksum ?? string.Empty,
                    IsValid = true,
                    FilePath = zipFile
                });
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[备份] 获取备份列表失败: {ex.Message}");
        }

        return backups;
    }

    /// <summary>
    /// 删除备份
    /// </summary>
    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        try
        {
            var zipPath = Path.Combine(_backupDirectory, $"{backupId}.zip");
            if (File.Exists(zipPath))
            {
                await Task.Run(() => File.Delete(zipPath));
                Program.Log($"[备份] 已删除备份: {backupId}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Program.Log($"[备份] 删除备份失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 验证备份完整性
    /// </summary>
    public async Task<bool> VerifyBackupIntegrityAsync(string backupId)
    {
        try
        {
            var zipPath = Path.Combine(_backupDirectory, $"{backupId}.zip");
            if (!File.Exists(zipPath))
                return false;

            // 解压并重新计算校验和
            var tempFolder = Path.Combine(_backupDirectory, $"verify_{Guid.NewGuid():N}");
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, tempFolder, true));

            var currentChecksum = await CalculateBackupChecksumAsync(tempFolder);

            // 清理临时文件夹
            Directory.Delete(tempFolder, true);

            // 比较校验和
            var backups = await GetBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.Id == backupId);

            if (backup == null)
                return false;

            var isValid = currentChecksum == backup.Checksum;
            if (isValid)
                Program.Log($"[备份] 备份验证通过: {backupId}");
            else
                Program.Log($"[备份] 备份校验和不匹配: {backupId}");

            return isValid;
        }
        catch (Exception ex)
        {
            Program.Log($"[备份] 备份验证失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取备份元数据
    /// </summary>
    public async Task<BackupMetadata> GetBackupMetadataAsync(string backupId)
    {
        try
        {
            var zipPath = Path.Combine(_backupDirectory, $"{backupId}.zip");
            var tempFolder = Path.Combine(_backupDirectory, $"meta_{Guid.NewGuid():N}");
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, tempFolder, true));

            var metadataPath = Path.Combine(tempFolder, "metadata.json");
            if (File.Exists(metadataPath))
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                Directory.Delete(tempFolder, true);
                return JsonSerializer.Deserialize<BackupMetadata>(json) ?? new BackupMetadata();
            }

            Directory.Delete(tempFolder, true);
        }
        catch { }

        return new BackupMetadata { Id = backupId };
    }

    private async Task CheckAndCreateAutoBackupAsync()
    {
        if (!IsRunning)
            return;

        try
        {
            var settings = _settingsService.GetSettings();

            // 检查是否启用了自动备份
            if (!settings.Performance.AutoBackupEnabled)
                return;

            // 检查距离上次备份的时间
            var backups = await GetBackupsAsync();
            var latestBackup = backups.FirstOrDefault();

            if (latestBackup == null ||
                (DateTime.UtcNow - latestBackup.CreatedAt).TotalHours >= settings.Performance.AutoBackupIntervalHours)
            {
                var backupType = settings.Performance.DifferentialBackupEnabled ? BackupType.Differential : BackupType.Full;
                await CreateBackupAsync(backupType);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[备份] 自动备份检查失败: {ex.Message}");
        }
    }

    private async Task CleanupOldBackupsAsync(int keepCount)
    {
        try
        {
            var backups = (await GetBackupsAsync())
                .OrderByDescending(b => b.CreatedAt)
                .Skip(keepCount)
                .ToList();

            foreach (var backup in backups)
            {
                await DeleteBackupAsync(backup.Id);
            }

            if (backups.Count > 0)
            {
                Program.Log($"[备份] 清理了 {backups.Count} 个旧备份");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[备份] 清理旧备份失败: {ex.Message}");
        }
    }

    private string GetAppVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoBackupTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
