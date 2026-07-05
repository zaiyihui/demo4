using System;
using System.Diagnostics;
using System.Threading;

namespace ComputerCompanion.Services;

/// <summary>
/// 性能监控和优化帮助类
/// 提供性能计数器、内存监控和资源管理功能
/// </summary>
public static class PerformanceHelper
{
    private static readonly Stopwatch _uptimeStopwatch = Stopwatch.StartNew();
    private static long _totalOperations;
    private static long _errorCount;
    private static long _lastGcMemory;

    /// <summary>
    /// 获取应用程序运行时间
    /// </summary>
    public static TimeSpan Uptime => _uptimeStopwatch.Elapsed;

    /// <summary>
    /// 获取总操作次数
    /// </summary>
    public static long TotalOperations => Interlocked.Read(ref _totalOperations);

    /// <summary>
    /// 获取错误计数
    /// </summary>
    public static long ErrorCount => Interlocked.Read(ref _errorCount);

    /// <summary>
    /// 增加操作计数
    /// </summary>
    public static void IncrementOperations()
    {
        Interlocked.Increment(ref _totalOperations);
    }

    /// <summary>
    /// 增加错误计数
    /// </summary>
    public static void IncrementErrors()
    {
        Interlocked.Increment(ref _errorCount);
    }

    /// <summary>
    /// 获取当前进程内存使用量（MB）
    /// </summary>
    public static double GetProcessMemoryMB()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64 / (1024.0 * 1024.0);
    }

    /// <summary>
    /// 获取GC内存使用量（MB）
    /// </summary>
    public static double GetGCMemoryMB()
    {
        return GC.GetTotalMemory(false) / (1024.0 * 1024.0);
    }

    /// <summary>
    /// 获取可用物理内存（MB）
    /// </summary>
    public static double GetAvailableMemoryMB()
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            var memInfo = GC.GetGCMemoryInfo();
            return memInfo.MemoryLoadBytes / (1024.0 * 1024.0);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取当前GC内存使用量（MB）
    /// 注意：不执行强制GC，依赖.NET运行时自动垃圾回收
    /// </summary>
    public static double CollectGarbage()
    {
        var current = GC.GetTotalMemory(false);
        _lastGcMemory = current;
        return current / (1024.0 * 1024.0);
    }

    /// <summary>
    /// 检查是否需要执行垃圾回收（内存使用超过阈值）
    /// </summary>
    public static bool ShouldCollectGarbage(double thresholdMB = 500)
    {
        return GetGCMemoryMB() > thresholdMB;
    }

    /// <summary>
    /// 获取性能统计信息
    /// </summary>
    public static PerformanceStats GetStats()
    {
        return new PerformanceStats
        {
            Uptime = Uptime,
            TotalOperations = TotalOperations,
            ErrorCount = ErrorCount,
            ProcessMemoryMB = GetProcessMemoryMB(),
            GCMemoryMB = GetGCMemoryMB(),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// 测量代码执行时间
    /// </summary>
    public static ExecutionResult MeasureExecution(Action action, string? name = null)
    {
        var sw = Stopwatch.StartNew();
        Exception? exception = null;
        
        try
        {
            action();
        }
        catch (Exception ex)
        {
            exception = ex;
            IncrementErrors();
        }
        finally
        {
            sw.Stop();
            IncrementOperations();
        }

        return new ExecutionResult
        {
            Name = name ?? "Unnamed",
            Duration = sw.Elapsed,
            Success = exception == null,
            Exception = exception
        };
    }

    /// <summary>
    /// 测量异步代码执行时间
    /// </summary>
    public static async System.Threading.Tasks.Task<ExecutionResult> MeasureExecutionAsync(
        Func<System.Threading.Tasks.Task> action, 
        string? name = null)
    {
        var sw = Stopwatch.StartNew();
        Exception? exception = null;
        
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            exception = ex;
            IncrementErrors();
        }
        finally
        {
            sw.Stop();
            IncrementOperations();
        }

        return new ExecutionResult
        {
            Name = name ?? "Unnamed",
            Duration = sw.Elapsed,
            Success = exception == null,
            Exception = exception
        };
    }
}

/// <summary>
/// 性能统计信息
/// </summary>
public class PerformanceStats
{
    public TimeSpan Uptime { get; set; }
    public long TotalOperations { get; set; }
    public long ErrorCount { get; set; }
    public double ProcessMemoryMB { get; set; }
    public double GCMemoryMB { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }

    public double ErrorRate => TotalOperations > 0 ? (double)ErrorCount / TotalOperations : 0;

    public override string ToString()
    {
        return $"运行时间: {Uptime:hh\\:mm\\:ss}, " +
               $"操作: {TotalOperations}, " +
               $"错误: {ErrorCount} ({ErrorRate:P2}), " +
               $"内存: {ProcessMemoryMB:F1}MB (GC: {GCMemoryMB:F1}MB), " +
               $"GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections}";
    }
}

/// <summary>
/// 执行结果
/// </summary>
public class ExecutionResult
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }

    public override string ToString()
    {
        return Success 
            ? $"[{Name}] 成功 - {Duration.TotalMilliseconds:F2}ms"
            : $"[{Name}] 失败 - {Duration.TotalMilliseconds:F2}ms - {Exception?.Message}";
    }
}
