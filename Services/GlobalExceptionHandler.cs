using ComputerCompanion.Core.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

/// <summary>
/// 全局异常处理器
/// 提供统一的异常捕获、记录和处理机制
/// </summary>
public class GlobalExceptionHandler
{
    private static GlobalExceptionHandler? _instance;
    private static readonly object _lock = new object();
    
    private readonly ILogService? _logService;
    private int _handledCount;

    public event EventHandler<ExceptionEventArgs>? ExceptionHandled;

    private GlobalExceptionHandler(ILogService? logService = null)
    {
        _logService = logService;
    }

    public static GlobalExceptionHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new GlobalExceptionHandler();
                }
            }
            return _instance;
        }
    }

    public static void Initialize(ILogService? logService = null)
    {
        lock (_lock)
        {
            _instance = new GlobalExceptionHandler(logService);
        }
    }

    /// <summary>
    /// 处理异常
    /// </summary>
    public void HandleException(Exception ex, string? context = null)
    {
        Interlocked.Increment(ref _handledCount);

        var message = string.IsNullOrEmpty(context) 
            ? $"全局异常: {ex.GetType().Name}: {ex.Message}"
            : $"[{context}] 异常: {ex.GetType().Name}: {ex.Message}";

        _logService?.Error(message, ex);
        Program.Log(message);

        // 触发事件
        ExceptionHandled?.Invoke(this, new ExceptionEventArgs(ex, context));
    }

    /// <summary>
    /// 安全执行操作，自动捕获异常
    /// </summary>
    public bool TryExecute(Action action, string? context = null)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            return false;
        }
    }

    /// <summary>
    /// 安全执行异步操作，自动捕获异常
    /// </summary>
    public async Task<bool> TryExecuteAsync(Func<Task> action, string? context = null)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            return false;
        }
    }

    /// <summary>
    /// 安全执行并返回结果
    /// </summary>
    public T? TryExecute<T>(Func<T> action, T? defaultValue = default, string? context = null)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全执行异步操作并返回结果
    /// </summary>
    public async Task<T?> TryExecuteAsync<T>(Func<Task<T>> action, T? defaultValue = default, string? context = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            return defaultValue;
        }
    }

    /// <summary>
    /// 获取已处理的异常计数
    /// </summary>
    public int HandledCount => _handledCount;

    /// <summary>
    /// 重置计数器
    /// </summary>
    public void ResetCount()
    {
        Interlocked.Exchange(ref _handledCount, 0);
    }
}

/// <summary>
/// 异常事件参数
/// </summary>
public class ExceptionEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string? Context { get; }
    public DateTime Timestamp { get; }

    public ExceptionEventArgs(Exception exception, string? context = null)
    {
        Exception = exception;
        Context = context;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// 异常处理扩展方法
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// 获取异常的完整消息链
    /// </summary>
    public static string GetFullMessage(this Exception ex)
    {
        var messages = new System.Text.StringBuilder();
        var current = ex;
        while (current != null)
        {
            messages.AppendLine(current.Message);
            current = current.InnerException;
        }
        return messages.ToString().TrimEnd();
    }

    /// <summary>
    /// 获取异常的堆栈跟踪（包含所有内部异常）
    /// </summary>
    public static string GetFullStackTrace(this Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        var current = ex;
        var level = 0;
        while (current != null)
        {
            if (level > 0)
                sb.AppendLine($"--- 内部异常 {level} ---");
            sb.AppendLine(current.StackTrace);
            current = current.InnerException;
            level++;
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 判断异常是否可重试
    /// </summary>
    public static bool IsRetryable(this Exception ex)
    {
        return ex is TimeoutException ||
               ex is OperationCanceledException ||
               ex is System.IO.IOException ||
               ex is System.Net.WebException;
    }
}
