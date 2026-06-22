using System;
using System.Collections.Generic;

namespace ComputerCompanion.Core.Models;

/// <summary>
/// 备份信息
/// </summary>
public class BackupInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BackupType Type { get; set; } = BackupType.Full;
    public long SizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// 备份类型
/// </summary>
public enum BackupType
{
    Full,           // 完全备份
    Differential,  // 差异备份
    Incremental     // 增量备份
}

/// <summary>
/// 备份结果
/// </summary>
public class BackupResult
{
    public bool Success { get; set; }
    public string? BackupId { get; set; }
    public string? ErrorMessage { get; set; }
    public long SizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public int FilesBackedUp { get; set; }
}

/// <summary>
/// 备份元数据
/// </summary>
public class BackupMetadata
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public BackupType Type { get; set; }
    public long SizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 敏感信息模式
/// </summary>
public class SensitivePattern
{
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = "***";
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 性能指标
/// </summary>
public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // CPU
    public double CpuUsagePercent { get; set; }
    public double CpuTemperature { get; set; }

    // GPU
    public double GpuUsagePercent { get; set; }
    public double GpuTemperature { get; set; }
    public double GpuMemoryUsedMB { get; set; }
    public double GpuMemoryTotalMB { get; set; }

    // 内存
    public double MemoryUsedMB { get; set; }
    public double MemoryTotalMB { get; set; }
    public double MemoryUsagePercent => MemoryTotalMB > 0 ? (MemoryUsedMB / MemoryTotalMB) * 100 : 0;

    // 磁盘
    public double DiskReadMB { get; set; }
    public double DiskWriteMB { get; set; }

    // 网络
    public double NetworkDownloadMBps { get; set; }
    public double NetworkUploadMBps { get; set; }

    // FPS
    public double Fps { get; set; }

    // 响应时间
    public double AverageResponseTimeMs { get; set; }
    public double P95ResponseTimeMs { get; set; }
    public double P99ResponseTimeMs { get; set; }

    // 吞吐量
    public int RequestsPerSecond { get; set; }
    public int ErrorsPerMinute { get; set; }
}

/// <summary>
/// 指标类型
/// </summary>
public enum MetricType
{
    Gauge,      // 瞬时值
    Counter,     // 计数器
    Histogram,   // 直方图
    Summary      // 摘要
}

/// <summary>
/// 指标数据点
/// </summary>
public class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public string Unit { get; set; } = string.Empty;
    public MetricType MetricType { get; set; }
}

/// <summary>
/// 告警规则
/// </summary>
public class AlertRule
{
    public string Name { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public ComparisonOperator Operator { get; set; }
    public double Threshold { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public List<string> NotificationChannels { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 比较运算符
/// </summary>
public enum ComparisonOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equal,
    NotEqual
}

/// <summary>
/// 告警严重级别
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// 插件信息
/// </summary>
public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public PluginStatus Status { get; set; } = PluginStatus.Disabled;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    public bool IsBuiltIn { get; set; }
}

/// <summary>
/// 插件状态
/// </summary>
public enum PluginStatus
{
    Disabled,
    Enabled,
    Running,
    Error,
    Updating
}

/// <summary>
/// 插件更新信息
/// </summary>
public class PluginUpdateInfo
{
    public string PluginId { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public bool IsUpdateAvailable { get; set; }
    public string? Changelog { get; set; }
    public long SizeBytes { get; set; }
    public string? DownloadUrl { get; set; }
}

/// <summary>
/// 文化信息模型
/// </summary>
public class CultureInfoModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public bool IsRTL { get; set; }
    public string FlagEmoji { get; set; } = string.Empty;
}

/// <summary>
/// 文化变更事件参数
/// </summary>
public class CultureChangedEventArgs : EventArgs
{
    public string OldCulture { get; set; } = string.Empty;
    public string NewCulture { get; set; } = string.Empty;
}

/// <summary>
/// 指标更新事件参数
/// </summary>
public class MetricsUpdatedEventArgs : EventArgs
{
    public PerformanceMetrics Metrics { get; set; } = new();
    public IEnumerable<AlertTriggeredEventArgs>? TriggeredAlerts { get; set; }
}

/// <summary>
/// 告警触发事件参数
/// </summary>
public class AlertTriggeredEventArgs : EventArgs
{
    public AlertRule Rule { get; set; } = new();
    public double CurrentValue { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 同步状态
/// </summary>
public enum SyncStatus
{
    Idle,
    Syncing,
    Success,
    Failed,
    Conflict
}

/// <summary>
/// 同步结果
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int ItemsUploaded { get; set; }
    public int ItemsDownloaded { get; set; }
    public int ConflictsResolved { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// 同步冲突
/// </summary>
public class SyncConflict
{
    public string Id { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string LocalValue { get; set; } = string.Empty;
    public string RemoteValue { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime RemoteTimestamp { get; set; }
}

/// <summary>
/// 冲突解决方案
/// </summary>
public enum ConflictResolution
{
    KeepLocal,
    KeepRemote,
    KeepBoth,
    KeepLatest,
    Manual
}

/// <summary>
/// 同步事件参数
/// </summary>
public class SyncEventArgs : EventArgs
{
    public SyncStatus Status { get; set; }
    public string? Message { get; set; }
    public SyncResult? Result { get; set; }
}

/// <summary>
/// AI建议
/// </summary>
public class AISuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SuggestionCategory Category { get; set; }
    public double Confidence { get; set; }
    public string? ActionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 建议类别
/// </summary>
public enum SuggestionCategory
{
    Performance,
    Maintenance,
    Security,
    Productivity,
    Customization
}

/// <summary>
/// 硬件分析结果
/// </summary>
public class HardwareAnalysis
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public HealthStatus OverallHealth { get; set; }
    public List<ComponentHealth> Components { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// 健康状态
/// </summary>
public enum HealthStatus
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// 组件健康状态
/// </summary>
public class ComponentHealth
{
    public string ComponentName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public double HealthScore { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// 异常预测
/// </summary>
public class AnomalyPrediction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ComponentName { get; set; } = string.Empty;
    public AnomalyType Type { get; set; }
    public double Probability { get; set; }
    public DateTime PredictedTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Mitigations { get; set; } = new();
}

/// <summary>
/// 异常类型
/// </summary>
public enum AnomalyType
{
    Temperature,
    Performance,
    Memory,
    Disk,
    Network,
    Power
}

/// <summary>
/// 隐私级别
/// </summary>
public enum PrivacyLevel
{
    Maximum,   // 所有数据本地处理
    High,       // 仅匿名数据上传
    Medium,     // 必要数据上传
    Low         // 完整功能
}
