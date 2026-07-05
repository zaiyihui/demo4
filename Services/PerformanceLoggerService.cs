using ComputerCompanion.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

public interface IPerformanceLoggerService : IDisposable
{
    bool IsRecording { get; }
    string LogFilePath { get; }
    
    void StartRecording();
    void StopRecording();
    void SetLogPath(string path);
    void AddLogEntry(PerformanceLogEntry entry);
}

public class PerformanceLogEntry
{
    public DateTime Timestamp { get; set; }
    public float? Fps { get; set; }
    public float? Fps1PercentLow { get; set; }
    public float? Fps01PercentLow { get; set; }
    public float? CpuUsage { get; set; }
    public float? CpuTemp { get; set; }
    public float? GpuUsage { get; set; }
    public float? GpuTemp { get; set; }
    public float? GpuVramUsed { get; set; }
    public float? MemoryUsed { get; set; }
    public float? MemoryTotal { get; set; }
    public float? NetworkLatency { get; set; }
}

public class PerformanceLoggerService : IPerformanceLoggerService
{
    private readonly object _lock = new();
    private readonly Queue<PerformanceLogEntry> _entryQueue = new();
    private readonly ISettingsService _settingsService;
    private Task? _writerTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRecording;
    private string _logFilePath = string.Empty;
    private StreamWriter? _streamWriter;

    public bool IsRecording => _isRecording;
    public string LogFilePath => _logFilePath;

    public PerformanceLoggerService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _logFilePath = GetDefaultLogPath();
    }

    private string GetDefaultLogPath()
    {
        try
        {
            var dataStorageService = App.ServiceProvider.GetService(typeof(IDataStorageService)) as IDataStorageService;
            if (dataStorageService != null)
            {
                var logDir = dataStorageService.GetLogPath();
                return Path.Combine(logDir, $"performance_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
        }
        catch { }
        
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ComputerCompanion", "Logs", $"performance_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    public void StartRecording()
    {
        lock (_lock)
        {
            if (_isRecording)
                return;

            _isRecording = true;
            _logFilePath = GetDefaultLogPath();
            
            try
            {
                var logDir = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir!);

                _streamWriter = new StreamWriter(_logFilePath, false, Encoding.UTF8);
                WriteHeader();
                
                _cancellationTokenSource = new CancellationTokenSource();
                _writerTask = Task.Run(ProcessQueue, _cancellationTokenSource.Token);
                
                Program.Log($"[日志] 性能日志录制已启动: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Program.Log($"[日志] 启动性能日志失败: {ex.Message}");
                _isRecording = false;
            }
        }
    }

    public void StopRecording()
    {
        lock (_lock)
        {
            if (!_isRecording)
                return;

            _isRecording = false;
            
            try
            {
                _cancellationTokenSource?.Cancel();
                _writerTask?.Wait(2000);
                
                FlushQueue();
                
                _streamWriter?.Close();
                _streamWriter?.Dispose();
                _streamWriter = null;
                
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                Program.Log($"[日志] 性能日志录制已停止");
            }
            catch (Exception ex)
            {
                Program.Log($"[日志] 停止性能日志失败: {ex.Message}");
            }
        }
    }

    public void SetLogPath(string path)
    {
        lock (_lock)
        {
            if (_isRecording)
                return;
            
            _logFilePath = path;
        }
    }

    public void AddLogEntry(PerformanceLogEntry entry)
    {
        if (!_isRecording)
            return;

        lock (_lock)
        {
            if (_entryQueue.Count < 1000)
            {
                _entryQueue.Enqueue(entry);
            }
        }
    }

    private void WriteHeader()
    {
        _streamWriter?.WriteLine("时间戳,FPS,FPS_1%_Low,FPS_0.1%_Low,CPU_使用率%,CPU_温度°C,GPU_使用率%,GPU_温度°C,GPU_VRAM_GB,内存_使用GB,内存_总GB,网络延迟_ms");
    }

    private void WriteEntry(PerformanceLogEntry entry)
    {
        _streamWriter?.WriteLine(
            $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
            $"{entry.Fps?.ToString("F1") ?? "N/A"}," +
            $"{entry.Fps1PercentLow?.ToString("F1") ?? "N/A"}," +
            $"{entry.Fps01PercentLow?.ToString("F1") ?? "N/A"}," +
            $"{entry.CpuUsage?.ToString("F1") ?? "N/A"}," +
            $"{entry.CpuTemp?.ToString("F0") ?? "N/A"}," +
            $"{entry.GpuUsage?.ToString("F1") ?? "N/A"}," +
            $"{entry.GpuTemp?.ToString("F0") ?? "N/A"}," +
            $"{entry.GpuVramUsed?.ToString("F2") ?? "N/A"}," +
            $"{entry.MemoryUsed?.ToString("F2") ?? "N/A"}," +
            $"{entry.MemoryTotal?.ToString("F2") ?? "N/A"}," +
            $"{entry.NetworkLatency?.ToString("F0") ?? "N/A"}"
        );
    }

    private async Task ProcessQueue()
    {
        var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
        
        while (!token.IsCancellationRequested && _isRecording)
        {
            try
            {
                PerformanceLogEntry? entry = null;
                
                lock (_lock)
                {
                    if (_entryQueue.Count > 0)
                    {
                        entry = _entryQueue.Dequeue();
                    }
                }
                
                if (entry != null)
                {
                    WriteEntry(entry);
                }
                else
                {
                    await Task.Delay(50, token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Program.Log($"[日志] 写入日志失败: {ex.Message}");
            }
        }
    }

    private void FlushQueue()
    {
        lock (_lock)
        {
            while (_entryQueue.Count > 0)
            {
                var entry = _entryQueue.Dequeue();
                WriteEntry(entry);
            }
        }
        
        _streamWriter?.Flush();
    }

    public void Dispose()
    {
        StopRecording();
        GC.SuppressFinalize(this);
    }
}
