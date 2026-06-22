using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Core.Services;
using Xunit;

namespace ComputerCompanion.Tests.Services;

/// <summary>
/// 日志服务单元测试
/// </summary>
public class LogServiceTests : IDisposable
{
    private readonly LogService _logService;
    private readonly string _testLogDirectory;
    private readonly List<LogEntry> _capturedLogs = new();

    public LogServiceTests()
    {
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"log_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testLogDirectory);

        _logService = new LogService(_testLogDirectory);

        // 添加测试Sink来捕获日志
        _logService.AddSink(new TestLogSink(_capturedLogs));
    }

    [Fact]
    public async Task InitializeAsync_Success_CompletesWithoutError()
    {
        // Act
        await _logService.InitializeAsync();

        // Assert
        Assert.True(_logService.IsInitialized);
    }

    [Fact]
    public async Task StartAsync_AfterInit_StartsSuccessfully()
    {
        // Arrange
        await _logService.InitializeAsync();

        // Act
        await _logService.StartAsync();

        // Assert
        Assert.True(_logService.IsRunning);
    }

    [Fact]
    public void Log_WithDifferentLevels_WritesCorrectly()
    {
        // Arrange
        _logService.CurrentLogLevel = LogLevel.Debug;

        // Act
        _logService.Trace("Trace message");
        _logService.Debug("Debug message");
        _logService.Info("Info message");
        _logService.Warning("Warning message");
        _logService.Error("Error message");

        // Assert
        Assert.Equal(5, _capturedLogs.Count);
        Assert.Equal(LogLevel.Trace, _capturedLogs[0].Level);
        Assert.Equal(LogLevel.Error, _capturedLogs[4].Level);
    }

    [Fact]
    public void Log_WithSensitiveData_SanitizesCorrectly()
    {
        // Arrange
        _logService.CurrentLogLevel = LogLevel.Information;

        // Act
        _logService.Info("Email: test@example.com");
        _logService.Info("Password: secret123");
        _logService.Info("IP: 192.168.1.100");
        _logService.Info("Phone: 13812345678");

        // Assert
        var emailLog = _capturedLogs.First(l => l.Message.Contains("Email"));
        var passwordLog = _capturedLogs.First(l => l.Message.Contains("Password"));
        var ipLog = _capturedLogs.First(l => l.Message.Contains("IP"));
        var phoneLog = _capturedLogs.First(l => l.Message.Contains("Phone"));

        Assert.DoesNotContain("test@example.com", emailLog.Message);
        Assert.DoesNotContain("secret123", passwordLog.Message);
        Assert.DoesNotContain("192.168", ipLog.Message);
        Assert.DoesNotContain("13812345678", phoneLog.Message);
    }

    [Fact]
    public void Log_WithException_IncludesExceptionDetails()
    {
        // Arrange
        _logService.CurrentLogLevel = LogLevel.Error;
        var exception = new InvalidOperationException("Test exception");

        // Act
        _logService.Error("An error occurred", exception);

        // Assert
        var errorLog = _capturedLogs.First();
        Assert.NotNull(errorLog.Exception);
        Assert.Contains("InvalidOperationException", errorLog.Exception);
        Assert.Contains("Test exception", errorLog.Exception);
    }

    [Fact]
    public void Log_WithFormatArgs_FormatsCorrectly()
    {
        // Arrange
        _logService.CurrentLogLevel = LogLevel.Information;

        // Act
        _logService.Info("Value: {0}, Name: {1}", 42, "Test");

        // Assert
        var log = _capturedLogs.First();
        Assert.Contains("42", log.Message);
        Assert.Contains("Test", log.Message);
    }

    [Fact]
    public void Log_WithLevelBelowThreshold_DoesNotWrite()
    {
        // Arrange
        _logService.CurrentLogLevel = LogLevel.Warning;

        // Act
        _logService.Trace("Trace");
        _logService.Debug("Debug");
        _logService.Info("Info");

        // Assert
        Assert.Empty(_capturedLogs);
    }

    [Fact]
    public void AddSink_WithValidSink_AddsSuccessfully()
    {
        // Arrange
        var sink = new TestLogSink(new List<LogEntry>());

        // Act
        _logService.AddSink(sink);

        // Assert
        _logService.Info("Test");
        Assert.Single(_capturedLogs);
    }

    [Fact]
    public void RemoveSink_WithExistingSink_RemovesSuccessfully()
    {
        // Arrange
        var sink = new TestLogSink(_capturedLogs);
        _logService.AddSink(sink);

        // Act
        _logService.RemoveSink(sink);
        _logService.Info("Test");

        // Assert
        Assert.Empty(_capturedLogs);
    }

    [Fact]
    public async Task FlushAsync_Success_CompletesWithoutError()
    {
        // Act
        await _logService.FlushAsync();

        // Assert
        // 没有异常即成功
    }

    public void Dispose()
    {
        _logService?.Dispose();

        try
        {
            if (Directory.Exists(_testLogDirectory))
                Directory.Delete(_testLogDirectory, true);
        }
        catch { }
    }
}

/// <summary>
/// 测试用日志Sink
/// </summary>
internal class TestLogSink : ILogSink
{
    private readonly List<LogEntry> _logs;

    public string Name => "Test";
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    public TestLogSink(List<LogEntry> logs)
    {
        _logs = logs;
    }

    public Task WriteAsync(LogEntry entry)
    {
        _logs.Add(entry);
        return Task.CompletedTask;
    }

    public void Flush() { }
}
