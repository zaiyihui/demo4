using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// 云同步服务 - 实现数据同步、冲突解决、端到端加密
/// </summary>
public class CloudSyncService : ServiceBase, ICloudSyncService
{
    private readonly string _syncDirectory;
    private readonly string _pendingSyncFile;
    private readonly Dictionary<string, string> _localData = new();
    private readonly Dictionary<string, DateTime> _lastSyncTimes = new();

    private Timer? _syncTimer;
    private bool _isSyncing;

    public SyncStatus CurrentStatus { get; private set; } = SyncStatus.Idle;
    public DateTime? LastSyncTime { get; private set; }

    public event EventHandler<SyncEventArgs>? SyncStarted;
    public event EventHandler<SyncEventArgs>? SyncCompleted;
    public event EventHandler<SyncEventArgs>? SyncFailed;
    public event EventHandler<SyncEventArgs>? ConflictDetected;

    public CloudSyncService(string? syncDirectory = null)
    {
        _syncDirectory = syncDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ComputerCompanion", "Sync");

        _pendingSyncFile = Path.Combine(_syncDirectory, "pending_sync.json");

        Directory.CreateDirectory(_syncDirectory);
    }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();
        LoadPendingSyncData();
        Program.Log("[同步] 云同步服务已初始化");
        return Task.CompletedTask;
    }

    public override Task StartAsync()
    {
        base.StartAsync();

        // 设置定时同步（每5分钟）
        _syncTimer = new Timer(
            _ => _ = SyncAsync(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));

        Program.Log("[同步] 云同步已启动");
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _syncTimer?.Dispose();
        Program.Log("[同步] 云同步已停止");
        return base.StopAsync();
    }

    /// <summary>
    /// 执行同步
    /// </summary>
    public async Task<SyncResult> SyncAsync()
    {
        if (_isSyncing)
        {
            return new SyncResult
            {
                Success = false,
                ErrorMessage = "同步正在进行中"
            };
        }

        var startTime = DateTime.UtcNow;
        var result = new SyncResult();
        _isSyncing = true;
        CurrentStatus = SyncStatus.Syncing;

        try
        {
            SyncStarted?.Invoke(this, new SyncEventArgs { Status = SyncStatus.Syncing });
            Program.Log("[同步] 开始同步...");

            // 获取待同步的数据
            var pendingData = await GetPendingSyncDataAsync();

            // 模拟上传到云端（实际实现需要连接云服务）
            var uploaded = 0;
            foreach (var item in pendingData)
            {
                if (await UploadToCloudAsync(item.Key, item.Value))
                {
                    uploaded++;
                }
            }

            // 模拟从云端下载
            var downloaded = 0;
            var cloudData = await GetCloudDataAsync();
            foreach (var item in cloudData)
            {
                var localVersion = _localData.ContainsKey(item.Key) ? _localData[item.Key] : null;
                var remoteVersion = item.Value;

                // 检测冲突
                if (localVersion != null && localVersion != remoteVersion)
                {
                    var conflict = new SyncConflict
                    {
                        Id = Guid.NewGuid().ToString(),
                        DataType = item.Key,
                        LocalValue = localVersion,
                        RemoteValue = remoteVersion,
                        LocalTimestamp = _lastSyncTimes.GetValueOrDefault(item.Key, DateTime.MinValue),
                        RemoteTimestamp = DateTime.UtcNow
                    };

                    ConflictDetected?.Invoke(this, new SyncEventArgs
                    {
                        Status = SyncStatus.Conflict,
                        Message = $"发现冲突: {item.Key}"
                    });

                    // 自动解决冲突（保留最新）
                    var resolution = await ResolveConflictAsync(conflict);
                    if (resolution == ConflictResolution.KeepRemote)
                    {
                        _localData[item.Key] = remoteVersion;
                        downloaded++;
                    }
                }
                else if (localVersion == null)
                {
                    _localData[item.Key] = remoteVersion;
                    downloaded++;
                }
            }

            result.Success = true;
            result.ItemsUploaded = uploaded;
            result.ItemsDownloaded = downloaded;
            result.Duration = DateTime.UtcNow - startTime;

            LastSyncTime = DateTime.UtcNow;
            CurrentStatus = SyncStatus.Success;

            SyncCompleted?.Invoke(this, new SyncEventArgs
            {
                Status = SyncStatus.Success,
                Result = result
            });

            Program.Log($"[同步] 完成: 上传 {uploaded}, 下载 {downloaded}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            CurrentStatus = SyncStatus.Failed;

            SyncFailed?.Invoke(this, new SyncEventArgs
            {
                Status = SyncStatus.Failed,
                Message = ex.Message
            });

            Program.Log($"[同步] 失败: {ex.Message}");
        }
        finally
        {
            _isSyncing = false;
        }

        return result;
    }

    /// <summary>
    /// 上传数据
    /// </summary>
    public async Task<bool> UploadAsync(string dataType, string data)
    {
        try
        {
            // 加密数据
            var encryptedData = Encrypt(data);

            // 保存本地副本
            _localData[dataType] = data;

            // 保存到待同步文件
            await SavePendingSyncDataAsync(dataType, encryptedData);

            // 模拟上传到云端
            var cloudFile = Path.Combine(_syncDirectory, $"{dataType}.encrypted");
            await File.WriteAllTextAsync(cloudFile, encryptedData);

            _lastSyncTimes[dataType] = DateTime.UtcNow;
            Program.Log($"[同步] 已上传: {dataType}");
            return true;
        }
        catch (Exception ex)
        {
            Program.Log($"[同步] 上传失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 下载数据
    /// </summary>
    public async Task<string?> DownloadAsync(string dataType)
    {
        try
        {
            var cloudFile = Path.Combine(_syncDirectory, $"{dataType}.encrypted");
            if (!File.Exists(cloudFile))
            {
                return null;
            }

            var encryptedData = await File.ReadAllTextAsync(cloudFile);
            var decryptedData = Decrypt(encryptedData);

            Program.Log($"[同步] 已下载: {dataType}");
            return decryptedData;
        }
        catch (Exception ex)
        {
            Program.Log($"[同步] 下载失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解决冲突
    /// </summary>
    public async Task<ConflictResolution> ResolveConflictAsync(SyncConflict conflict)
    {
        // 默认策略：保留最新
        var resolution = ConflictResolution.KeepLatest;

        // 检查冲突解决策略文件
        var strategyFile = Path.Combine(_syncDirectory, "conflict_strategy.json");
        if (File.Exists(strategyFile))
        {
            var json = await File.ReadAllTextAsync(strategyFile);
            var strategy = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (strategy != null && strategy.TryGetValue(conflict.DataType, out var strategyValue))
            {
                if (Enum.TryParse<ConflictResolution>(strategyValue, out var parsed))
                {
                    resolution = parsed;
                }
            }
        }

        Program.Log($"[同步] 冲突解决: {conflict.DataType} -> {resolution}");
        return resolution;
    }

    /// <summary>
    /// 加密数据
    /// </summary>
    private string Encrypt(string plainText)
    {
        // 使用简单的加密作为示例（实际应使用更强的加密）
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = new byte[plainBytes.Length];

        for (int i = 0; i < plainBytes.Length; i++)
        {
            encryptedBytes[i] = (byte)(plainBytes[i] ^ 0x5A);
        }

        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// 解密数据
    /// </summary>
    private string Decrypt(string encryptedText)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedText);
        var plainBytes = new byte[encryptedBytes.Length];

        for (int i = 0; i < encryptedBytes.Length; i++)
        {
            plainBytes[i] = (byte)(encryptedBytes[i] ^ 0x5A);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    private async Task<Dictionary<string, string>> GetPendingSyncDataAsync()
    {
        if (!File.Exists(_pendingSyncFile))
            return new Dictionary<string, string>();

        try
        {
            var json = await File.ReadAllTextAsync(_pendingSyncFile);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private async Task SavePendingSyncDataAsync(string dataType, string encryptedData)
    {
        var pending = await GetPendingSyncDataAsync();
        pending[dataType] = encryptedData;
        var json = JsonConvert.SerializeObject(pending, Formatting.Indented);
        await File.WriteAllTextAsync(_pendingSyncFile, json);
    }

    private void LoadPendingSyncData()
    {
        var pendingFile = Path.Combine(_syncDirectory, "local_data.json");
        if (File.Exists(pendingFile))
        {
            try
            {
                var json = File.ReadAllText(pendingFile);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        _localData[item.Key] = item.Value;
                    }
                }
            }
            catch { }
        }
    }

    private async Task SaveLocalDataAsync()
    {
        var file = Path.Combine(_syncDirectory, "local_data.json");
        var json = JsonConvert.SerializeObject(_localData, Formatting.Indented);
        await File.WriteAllTextAsync(file, json);
    }

    private async Task<Dictionary<string, string>> GetCloudDataAsync()
    {
        // 模拟云端数据（实际应从云服务获取）
        await Task.CompletedTask;
        return new Dictionary<string, string>();
    }

    private async Task<bool> UploadToCloudAsync(string dataType, string encryptedData)
    {
        // 模拟上传
        await Task.CompletedTask;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _syncTimer?.Dispose();
            _ = SaveLocalDataAsync();
        }
        base.Dispose(disposing);
    }
}
