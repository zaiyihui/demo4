using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// 日志服务 - 实现日志脱敏、分级输出、轮转机制
/// </summary>
public class LogService : ServiceBase, ILogService
{
    private readonly string _logDirectory;
    private readonly object _writeLock = new object();
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly Timer _flushTimer;
    private readonly List<ILogSink> _sinks = new();
    private readonly List<SensitivePattern> _sensitivePatterns;

    private StreamWriter? _fileWriter;
    private DateTime _currentLogDate;
    private int _currentLogSize;
    private const int MaxLogSizeMB = 10;

    public LogLevel CurrentLogLevel { get; set; } = LogLevel.Debug;

    public event EventHandler<LogEntry>? LogWritten;

    public LogService(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ComputerCompanion", "Logs");

        // 初始化敏感信息模式
        _sensitivePatterns = InitializeSensitivePatterns();

        // 确保日志目录存在
        Directory.CreateDirectory(_logDirectory);

        // 设置刷新定时器（每秒刷新一次）
        _flushTimer = new Timer(
            _ => _ = FlushAsync(),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));

        // 添加默认控制台输出
        AddSink(new ConsoleLogSink());
    }

    private List<SensitivePattern> InitializeSensitivePatterns()
    {
        return new List<SensitivePattern>
        {
            // 邮箱
            new SensitivePattern
            {
                Name = "Email",
                Pattern = @"[\w.-]+@[\w.-]+\.\w+",
                Replacement = "***@***.***"
            },
            // 手机号（中国）
            new SensitivePattern
            {
                Name = "Phone",
                Pattern = @"1[3-9]\d{9}",
                Replacement = "138****5678"
            },
            // 身份证号
            new SensitivePattern
            {
                Name = "IDCard",
                Pattern = @"\d{17}[\dXx]",
                Replacement = "**************1234"
            },
            // IP地址
            new SensitivePattern
            {
                Name = "IPAddress",
                Pattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}",
                Replacement = "***.***.***.***"
            },
            // 密码字段
            new SensitivePattern
            {
                Name = "Password",
                Pattern = @"(password|pwd|passwd)[""\s:]*[=:][""\s]*[^\s""]+",
                Replacement = "$1=***",
                IsEnabled = true
            },
            // API密钥
            new SensitivePattern
            {
                Name = "APIKey",
                Pattern = @"(api[_-]?key|apikey|secret|token)[""\s:]*[=:][""\s]*[^\s""]+",
                Replacement = "$1=***"
            },
            // Bearer令牌
            new SensitivePattern
            {
                Name = "BearerToken",
                Pattern = @"Bearer\s+[^\s]+",
                Replacement = "Bearer ***"
            },
            // Windows文件路径
            new SensitivePattern
            {
                Name = "WindowsPath",
                Pattern = @"[A-Za-z]:\\[^\s]+",
                Replacement = "C:\\***"
            },
            // Linux文件路径
            new SensitivePattern
            {
                Name = "LinuxPath",
                Pattern = @"/[^\s]+",
                Replacement = "/***"
            },
            // 信用卡号
            new SensitivePattern
            {
                Name = "CreditCard",
                Pattern = @"\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}",
                Replacement = "****-****-****-****"
            },
            // 连接字符串
            new SensitivePattern
            {
                Name = "ConnectionString",
                Pattern = @"(connectionstring|connstr|connection_string)[""\s:]*[=:][""\s]*[^\s""]+",
                Replacement = "$1=***"
            }
        };
    }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();
        InitializeFileWriter();
        return Task.CompletedTask;
    }

    private void InitializeFileWriter()
    {
        lock (_writeLock)
        {
            // 关闭旧的writer
            _fileWriter?.Dispose();

            // 创建新的日志文件
            _currentLogDate = DateTime.UtcNow.Date;
            var logFileName = $"app_{_currentLogDate:yyyyMMdd}.log";
            var logFilePath = Path.Combine(_logDirectory, logFileName);

            _fileWriter = new StreamWriter(logFilePath, append: true)
            {
                AutoFlush = false
            };

            _currentLogSize = (int)(_fileWriter.BaseStream.Length);

            // 添加文件输出sink
            AddSink(new FileLogSink(logFilePath));
        }
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    public void Log(LogLevel level, string message, params object[] args)
    {
        if (level < CurrentLogLevel)
            return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = SanitizeMessage(string.Format(message, args)),
            Source = GetCallingSource(),
            ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
        };

        WriteEntry(entry);
    }

    public void Trace(string message, params object[] args) => Log(LogLevel.Trace, message, args);
    public void Debug(string message, params object[] args) => Log(LogLevel.Debug, message, args);
    public void Info(string message, params object[] args) => Log(LogLevel.Information, message, args);
    public void Warning(string message, params object[] args) => Log(LogLevel.Warning, message, args);

    public void Error(string message, Exception? ex = null, params object[] args)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Message = SanitizeMessage(string.Format(message, args)),
            Exception = ex != null ? SanitizeException(ex) : null,
            Source = GetCallingSource(),
            ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
        };

        WriteEntry(entry);
    }

    public void Critical(string message, Exception? ex = null, params object[] args)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Critical,
            Message = SanitizeMessage(string.Format(message, args)),
            Exception = ex != null ? SanitizeException(ex) : null,
            Source = GetCallingSource(),
            ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
        };

        WriteEntry(entry);
    }

    /// <summary>
    /// 敏感信息脱敏
    /// </summary>
    private string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var sanitized = message;

        foreach (var pattern in _sensitivePatterns.Where(p => p.IsEnabled))
        {
            try
            {
                sanitized = Regex.Replace(sanitized, pattern.Pattern, pattern.Replacement,
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            }
            catch
            {
                // 忽略正则表达式错误
            }
        }

        return sanitized;
    }

    /// <summary>
    /// 异常信息脱敏
    /// </summary>
    private string SanitizeException(Exception ex)
    {
        var sanitized = ex.Message;
        sanitized = SanitizeMessage(sanitized);

        // 如果有堆栈跟踪，也需要脱敏
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            sanitized += Environment.NewLine + "StackTrace: " + SanitizeMessage(ex.StackTrace);
        }

        return sanitized;
    }

    /// <summary>
    /// 写入日志条目
    /// </summary>
    private void WriteEntry(LogEntry entry)
    {
        _logQueue.Enqueue(entry);

        // 同步写入所有sink
        foreach (var sink in _sinks)
        {
            if (entry.Level >= sink.MinLevel)
            {
                try
                {
                    sink.WriteAsync(entry).Wait(TimeSpan.FromSeconds(1));
                }
                catch
                {
                    // 忽略sink写入错误
                }
            }
        }

        LogWritten?.Invoke(this, entry);
    }

    /// <summary>
    /// 获取调用源
    /// </summary>
    private string GetCallingSource()
    {
        var stackTrace = new System.Diagnostics.StackTrace(true);
        var frames = stackTrace.GetFrames();

        foreach (var frame in frames.Skip(3))
        {
            var method = frame.GetMethod();
            if (method != null && method.DeclaringType != null &&
                !method.DeclaringType.FullName!.StartsWith("ComputerCompanion.Core.Services"))
            {
                return $"{method.DeclaringType.Name}.{method.Name}";
            }
        }

        return "Unknown";
    }

    /// <summary>
    /// 添加日志sink
    /// </summary>
    public void AddSink(ILogSink sink)
    {
        if (!_sinks.Contains(sink))
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// 移除日志sink
    /// </summary>
    public void RemoveSink(ILogSink sink)
    {
        _sinks.Remove(sink);
    }

    /// <summary>
    /// 刷新日志缓冲区
    /// </summary>
    public async Task FlushAsync()
    {
        // 检查日期是否变化
        if (DateTime.UtcNow.Date != _currentLogDate)
        {
            InitializeFileWriter();
        }

        // 检查文件大小
        if (_currentLogSize > MaxLogSizeMB * 1024 * 1024)
        {
            await RotateLogFileAsync();
        }

        // 刷新所有sink
        foreach (var sink in _sinks)
        {
            try
            {
                sink.Flush();
            }
            catch
            {
                // 忽略刷新错误
            }
        }
    }

    private async Task RotateLogFileAsync()
    {
        lock (_writeLock)
        {
            _fileWriter?.Dispose();

            // 压缩旧的日志文件
            var oldLogFile = Path.Combine(_logDirectory, $"app_{_currentLogDate:yyyyMMdd}.log");
            var archiveFile = Path.Combine(_logDirectory, $"app_{_currentLogDate:yyyyMMdd}.log.gz");

            if (File.Exists(oldLogFile))
            {
                try
                {
                    using var input = File.OpenRead(oldLogFile);
                    using var output = File.Create(archiveFile);
                    using var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress);
                    input.CopyTo(gzip);
                    File.Delete(oldLogFile);
                }
                catch
                {
                    // 忽略压缩错误
                }
            }

            // 创建新的日志文件
            _currentLogDate = DateTime.UtcNow.Date;
            var newLogFile = Path.Combine(_logDirectory, $"app_{_currentLogDate:yyyyMMdd}.log");
            _fileWriter = new StreamWriter(newLogFile, append: false)
            {
                AutoFlush = false
            };
            _currentLogSize = 0;
        }

        await Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _flushTimer?.Dispose();
            _fileWriter?.Flush();
            _fileWriter?.Dispose();

            foreach (var sink in _sinks)
            {
                sink.Flush();
                if (sink is IDisposable disposable)
                    disposable.Dispose();
            }
            _sinks.Clear();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// 控制台日志输出
/// </summary>
public class ConsoleLogSink : ILogSink
{
    public string Name => "Console";
    public LogLevel MinLevel { get; set; } = LogLevel.Information;

    public Task WriteAsync(LogEntry entry)
    {
        var color = GetConsoleColor(entry.Level);
        Console.ForegroundColor = color;

        var prefix = entry.Level switch
        {
            LogLevel.Trace => "[TRC]",
            LogLevel.Debug => "[DBG]",
            LogLevel.Information => "[INF]",
            LogLevel.Warning => "[WRN]",
            LogLevel.Error => "[ERR]",
            LogLevel.Critical => "[CRT]",
            _ => "[LOG]"
        };

        Console.WriteLine($"{prefix} {entry.Timestamp:HH:mm:ss.fff} {entry.Message}");

        Console.ResetColor();
        return Task.CompletedTask;
    }

    private ConsoleColor GetConsoleColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.DarkGray,
        LogLevel.Information => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };

    public void Flush() { }
}

/// <summary>
/// 文件日志输出
/// </summary>
public class FileLogSink : ILogSink
{
    private readonly string _filePath;
    private readonly object _lock = new object();

    public string Name => "File";
    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    public FileLogSink(string filePath)
    {
        _filePath = filePath;
    }

    public Task WriteAsync(LogEntry entry)
    {
        lock (_lock)
        {
            try
            {
                var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Source}] {entry.Message}";

                if (!string.IsNullOrEmpty(entry.Exception))
                {
                    line += Environment.NewLine + entry.Exception;
                }

                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
            catch
            {
                // 忽略写入错误
            }
        }

        return Task.CompletedTask;
    }

    public void Flush() { }
}
