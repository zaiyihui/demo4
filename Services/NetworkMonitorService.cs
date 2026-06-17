using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Timers;

namespace ComputerCompanion.Services;

public class NetworkMonitorService : INetworkMonitorService
{
    private System.Timers.Timer? _timer;
    private bool _isRunning;
    private bool _isDisposed;
    
    private long _lastBytesReceived = 0;
    private long _lastBytesSent = 0;
    private DateTime _lastNetworkUpdate = DateTime.Now;

    public float? NetworkUpload { get; private set; }
    public float? NetworkDownload { get; private set; }

    public event Action? NetworkDataUpdated;

    public void Start(int intervalMs = 1000)
    {
        if (_isRunning || _isDisposed)
            return;

        _isRunning = true;
        Program.Log("[网络] 启动网络监控服务");

        try
        {
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }
        catch (Exception ex)
        {
            Program.Log($"[网络] 定时器启动失败: {ex.Message}");
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            UpdateNetworkData();
        }
        catch (Exception ex)
        {
            Program.Log($"[网络] 更新网络数据失败: {ex.Message}");
        }
    }

    private void UpdateNetworkData()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && 
                           !n.Description.Contains("Loopback") &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (var iface in interfaces)
            {
                var stats = iface.GetIPStatistics();
                totalBytesReceived += stats.BytesReceived;
                totalBytesSent += stats.BytesSent;
            }

            var now = DateTime.Now;
            var elapsed = (now - _lastNetworkUpdate).TotalSeconds;

            if (elapsed > 0 && _lastBytesReceived > 0)
            {
                var downloadDiff = totalBytesReceived - _lastBytesReceived;
                var uploadDiff = totalBytesSent - _lastBytesSent;
                
                if (downloadDiff >= 0 && uploadDiff >= 0)
                {
                    NetworkDownload = Math.Max(0, (float)(downloadDiff / elapsed / 1024 / 1024));
                    NetworkUpload = Math.Max(0, (float)(uploadDiff / elapsed / 1024 / 1024));
                }
            }

            _lastBytesReceived = totalBytesReceived;
            _lastBytesSent = totalBytesSent;
            _lastNetworkUpdate = now;

            NetworkDataUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Program.Log($"[网络] 更新网络数据失败: {ex.Message}");
        }
    }

    public void Stop()
    {
        _isRunning = false;
        
        _timer?.Stop();
        _timer?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
        GC.SuppressFinalize(this);
        Program.Log("[网络] 网络监控服务已释放");
    }
}