using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Plugins;

namespace ComputerCompanion.Core.Abstractions;

/// <summary>
/// 服务基类接口，定义所有服务的通用行为
/// </summary>
public interface IServiceBase
{
    /// <summary>
    /// 服务是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 服务是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 初始化服务
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 启动服务
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止服务
    /// </summary>
    Task StopAsync();
}

/// <summary>
/// 服务基类，提供通用服务功能
/// </summary>
public abstract class ServiceBase : IServiceBase, IDisposable
{
    private bool _isDisposed;
    private bool _isInitialized;
    private bool _isRunning;

    public bool IsInitialized => _isInitialized;
    public bool IsRunning => _isRunning;

    public virtual Task InitializeAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);

        _isInitialized = true;
        Program.Log($"[{GetType().Name}] 已初始化");
        return Task.CompletedTask;
    }

    public virtual Task StartAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);

        if (!_isInitialized)
            throw new InvalidOperationException("服务未初始化");

        _isRunning = true;
        Program.Log($"[{GetType().Name}] 已启动");
        return Task.CompletedTask;
    }

    public virtual Task StopAsync()
    {
        _isRunning = false;
        Program.Log($"[{GetType().Name}] 已停止");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 释放托管资源
        }
    }
}

/// <summary>
/// 配置备份服务接口
/// </summary>
public interface IBackupService : IServiceBase
{
    /// <summary>
    /// 创建备份
    /// </summary>
    Task<BackupResult> CreateBackupAsync(BackupType type = BackupType.Full);

    /// <summary>
    /// 恢复备份
    /// </summary>
    Task<bool> RestoreBackupAsync(string backupId);

    /// <summary>
    /// 获取所有备份列表
    /// </summary>
    Task<IEnumerable<BackupInfo>> GetBackupsAsync();

    /// <summary>
    /// 删除备份
    /// </summary>
    Task<bool> DeleteBackupAsync(string backupId);

    /// <summary>
    /// 验证备份完整性
    /// </summary>
    Task<bool> VerifyBackupIntegrityAsync(string backupId);

    /// <summary>
    /// 获取备份元数据
    /// </summary>
    Task<BackupMetadata> GetBackupMetadataAsync(string backupId);
}

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogService : IServiceBase
{
    /// <summary>
    /// 当前日志级别
    /// </summary>
    LogLevel CurrentLogLevel { get; set; }

    /// <summary>
    /// 记录日志
    /// </summary>
    void Log(LogLevel level, string message, params object[] args);

    /// <summary>
    /// 记录跟踪日志
    /// </summary>
    void Trace(string message, params object[] args);

    /// <summary>
    /// 记录调试日志
    /// </summary>
    void Debug(string message, params object[] args);

    /// <summary>
    /// 记录信息日志
    /// </summary>
    void Info(string message, params object[] args);

    /// <summary>
    /// 记录警告日志
    /// </summary>
    void Warning(string message, params object[] args);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    void Error(string message, Exception? ex = null, params object[] args);

    /// <summary>
    /// 记录严重错误日志
    /// </summary>
    void Critical(string message, Exception? ex = null, params object[] args);

    /// <summary>
    /// 添加日志sink
    /// </summary>
    void AddSink(ILogSink sink);

    /// <summary>
    /// 移除日志sink
    /// </summary>
    void RemoveSink(ILogSink sink);

    /// <summary>
    /// 刷新日志缓冲区
    /// </summary>
    Task FlushAsync();
}

/// <summary>
/// 日志输出接口
/// </summary>
public interface ILogSink
{
    string Name { get; }
    LogLevel MinLevel { get; set; }
    Task WriteAsync(LogEntry entry);
    void Flush();
}

/// <summary>
/// 性能监控服务接口
/// </summary>
public interface IPerformanceMonitorService : IServiceBase
{
    /// <summary>
    /// 当前性能指标
    /// </summary>
    PerformanceMetrics CurrentMetrics { get; }

    /// <summary>
    /// 记录自定义指标
    /// </summary>
    void RecordMetric(string name, double value, MetricType type = MetricType.Gauge);

    /// <summary>
    /// 开始计时
    /// </summary>
    IDisposable BeginTiming(string operationName);

    /// <summary>
    /// 获取历史指标
    /// </summary>
    IEnumerable<MetricDataPoint> GetHistoricalMetrics(string metricName, TimeSpan? duration = null);

    /// <summary>
    /// 添加告警规则
    /// </summary>
    void AddAlertRule(AlertRule rule);

    /// <summary>
    /// 移除告警规则
    /// </summary>
    void RemoveAlertRule(string ruleName);

    /// <summary>
    /// 指标更新事件
    /// </summary>
    event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;

    /// <summary>
    /// 告警触发事件
    /// </summary>
    event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;
}

/// <summary>
/// 插件服务接口
/// </summary>
public interface IPluginService : IServiceBase
{
    /// <summary>
    /// 所有已加载的插件
    /// </summary>
    IReadOnlyList<PluginInfo> LoadedPlugins { get; }

    /// <summary>
    /// 加载插件
    /// </summary>
    Task<IPlugin?> LoadPluginAsync(string pluginPath);

    /// <summary>
    /// 卸载插件
    /// </summary>
    Task<bool> UnloadPluginAsync(string pluginId);

    /// <summary>
    /// 启用插件
    /// </summary>
    Task<bool> EnablePluginAsync(string pluginId);

    /// <summary>
    /// 禁用插件
    /// </summary>
    Task<bool> DisablePluginAsync(string pluginId);

    /// <summary>
    /// 获取插件
    /// </summary>
    IPlugin? GetPlugin(string pluginId);

    /// <summary>
    /// 检查插件更新
    /// </summary>
    Task<PluginUpdateInfo?> CheckPluginUpdateAsync(string pluginId);

    /// <summary>
    /// 更新插件
    /// </summary>
    Task<bool> UpdatePluginAsync(string pluginId, string version);

    /// <summary>
    /// 插件事件
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginLoaded;
    event EventHandler<PluginEventArgs>? PluginUnloaded;
    event EventHandler<PluginEventArgs>? PluginEnabled;
    event EventHandler<PluginEventArgs>? PluginDisabled;
}

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService : IServiceBase
{
    /// <summary>
    /// 当前文化
    /// </summary>
    string CurrentCulture { get; }

    /// <summary>
    /// 支持的文化列表
    /// </summary>
    IReadOnlyList<CultureInfoModel> SupportedCultures { get; }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// 设置文化
    /// </summary>
    void SetCulture(string cultureCode);

    /// <summary>
    /// 文化变更事件
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
}

/// <summary>
/// 智能建议服务接口（基于规则引擎，非 AI/ML）
/// </summary>
public interface IInsightService : IServiceBase
{
    /// <summary>
    /// 隐私级别
    /// </summary>
    PrivacyLevel CurrentPrivacyLevel { get; set; }

    /// <summary>
    /// 获取智能建议（基于阈值规则的性能建议）
    /// </summary>
    Task<IEnumerable<AISuggestion>> GetSuggestionsAsync();

    /// <summary>
    /// 分析硬件状态
    /// </summary>
    Task<HardwareAnalysis> AnalyzeHardwareAsync();

    /// <summary>
    /// 预测异常
    /// </summary>
    Task<IEnumerable<AnomalyPrediction>> PredictAnomaliesAsync();

    /// <summary>
    /// 处理自然语言命令
    /// </summary>
    Task<string> ProcessCommandAsync(string command);
}
