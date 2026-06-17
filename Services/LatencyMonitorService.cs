using System;
using System.Net.NetworkInformation;
using System.Timers;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

public class LatencyMonitorService : ILatencyMonitorService
{
    private System.Timers.Timer? _pingTimer;
    private bool _isRunning;
    private bool _isDisposed;
    private int _pingLatencyMs = -1;

    private readonly string[] _pingHosts = { "223.5.5.5", "119.29.29.29", "8.8.8.8", "1.1.1.1" };

    public int? NetworkLatency { get; private set; }

    public event Action? LatencyUpdated;

    public void Start(int intervalMs = 3000)
    {
        if (_isRunning || _isDisposed)
            return;

        _isRunning = true;
        Program.Log("[延迟] 启动延迟监控服务");

        try
        {
            _pingTimer = new System.Timers.Timer(intervalMs);
            _pingTimer.Elapsed += async (_, _) => await MeasureLatencyAsync();
            _pingTimer.AutoReset = true;
            _pingTimer.Start();
            
            _ = MeasureLatencyAsync();
        }
        catch (Exception ex)
        {
            Program.Log($"[延迟] 定时器启动失败: {ex.Message}");
        }
    }

    private async Task MeasureLatencyAsync()
    {
        foreach (var host in _pingHosts)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 1000);

                if (reply.Status == IPStatus.Success)
                {
                    _pingLatencyMs = (int)reply.RoundtripTime;
                    NetworkLatency = _pingLatencyMs;
                    LatencyUpdated?.Invoke();
                    return;
                }
            }
            catch (Exception ex)
            {
                Program.Log($"[延迟] Ping {host} 失败: {ex.Message}");
            }
        }

        if (_pingLatencyMs < 0)
        {
            NetworkLatency = null;
            LatencyUpdated?.Invoke();
        }
    }

    public void Stop()
    {
        _isRunning = false;
        
        _pingTimer?.Stop();
        _pingTimer?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
        GC.SuppressFinalize(this);
        Program.Log("[延迟] 延迟监控服务已释放");
    }
}