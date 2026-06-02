using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComputerCompanion.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IHardwareMonitorService _monitor;
    private readonly ISettingsService _settingsService;
    private Settings _settings;

    [ObservableProperty]
    private string _cpuInfo = "CPU: --";

    [ObservableProperty]
    private string _gpuInfo = "GPU: --";

    [ObservableProperty]
    private string _memoryInfo = "内存: --";

    [ObservableProperty]
    private string _networkInfo = "网络: --";

    [ObservableProperty]
    private string _diskInfo = "磁盘: --";

    [ObservableProperty]
    private string _batteryInfo = "电池: --";

    [ObservableProperty]
    private string _latencyInfo = "延迟: --";

    [ObservableProperty]
    private bool _showGpu = true;

    [ObservableProperty]
    private bool _showBattery = true;

    [ObservableProperty]
    private bool _gameMode = false;

    [ObservableProperty]
    private double _cpuUsagePercent = 0;

    [ObservableProperty]
    private double _gpuUsagePercent = 0;

    [ObservableProperty]
    private double _memoryUsagePercent = 0;

    [ObservableProperty]
    private double _diskUsagePercent = 0;

    [ObservableProperty]
    private double _batteryLevelPercent = 0;

    public MainWindowViewModel(IHardwareMonitorService monitor, Settings settings, ISettingsService settingsService)
    {
        _monitor = monitor ?? throw new System.ArgumentNullException(nameof(monitor));
        _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new System.ArgumentNullException(nameof(settingsService));
        
        _monitor.DataUpdated += OnDataUpdated;
        
        ShowGpu = _monitor.HasGpu && _settings.DisplayContent.ShowGpu;
        ShowBattery = _monitor.HasBattery && _settings.DisplayContent.ShowBattery;
    }

    private void OnDataUpdated()
    {
        if (_settings.DisplayContent.ShowCpu)
        {
            CpuInfo = BuildCpuInfo();
            CpuUsagePercent = _monitor.CpuUsage ?? 0;
        }

        if (_settings.DisplayContent.ShowGpu && _monitor.HasGpu)
        {
            GpuInfo = BuildGpuInfo();
            GpuUsagePercent = _monitor.GpuUsage ?? 0;
        }

        if (_settings.DisplayContent.ShowMemory)
        {
            MemoryInfo = BuildMemoryInfo();
            if (_monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue)
            {
                MemoryUsagePercent = (_monitor.MemoryUsed.Value / _monitor.MemoryTotal.Value) * 100;
            }
        }

        if (_settings.DisplayContent.ShowNetwork)
        {
            NetworkInfo = BuildNetworkInfo();
            if (_monitor.NetworkLatency.HasValue)
            {
                LatencyInfo = $"{_monitor.NetworkLatency.Value}ms";
            }
            else
            {
                LatencyInfo = "延迟: --";
            }
        }

        if (_settings.DisplayContent.ShowDisk)
        {
            DiskInfo = BuildDiskInfo();
            if (_monitor.DiskFreeSpace.HasValue && _monitor.DiskTotalSpace.HasValue)
            {
                var used = _monitor.DiskTotalSpace.Value - _monitor.DiskFreeSpace.Value;
                DiskUsagePercent = (used / _monitor.DiskTotalSpace.Value) * 100;
            }
        }

        if (_settings.DisplayContent.ShowBattery && _monitor.HasBattery)
        {
            BatteryInfo = BuildBatteryInfo();
            BatteryLevelPercent = _monitor.BatteryLevel ?? 0;
        }
    }

    private readonly System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();

    private string BuildCpuInfo()
    {
        _stringBuilder.Clear();
        if (_monitor.CpuUsage.HasValue)
            _stringBuilder.AppendFormat("{0:F1}%", _monitor.CpuUsage.Value);

        if (_monitor.CpuTemp.HasValue)
        {
            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(" | ");
            _stringBuilder.AppendFormat("{0:F0}°C", _monitor.CpuTemp.Value);
        }

        if (_monitor.CpuFanSpeed.HasValue && _monitor.CpuFanSpeed.Value > 0)
        {
            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(" | ");
            _stringBuilder.AppendFormat("{0} RPM", _monitor.CpuFanSpeed.Value);
        }

        return _stringBuilder.Length > 0 ? _stringBuilder.ToString() : "CPU: --";
    }

    private string BuildGpuInfo()
    {
        _stringBuilder.Clear();
        if (_monitor.GpuUsage.HasValue)
            _stringBuilder.AppendFormat("{0:F1}%", _monitor.GpuUsage.Value);

        if (_monitor.GpuTemp.HasValue)
        {
            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(" | ");
            _stringBuilder.AppendFormat("{0:F0}°C", _monitor.GpuTemp.Value);
        }

        if (_monitor.GpuVramUsed.HasValue && _monitor.GpuVramTotal.HasValue)
        {
            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(" | ");
            _stringBuilder.AppendFormat("{0:F1}/{1:F1} GB", _monitor.GpuVramUsed.Value, _monitor.GpuVramTotal.Value);
        }

        if (_monitor.GpuFanSpeed.HasValue && _monitor.GpuFanSpeed.Value > 0)
        {
            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(" | ");
            _stringBuilder.AppendFormat("{0} RPM", _monitor.GpuFanSpeed.Value);
        }

        return _stringBuilder.Length > 0 ? _stringBuilder.ToString() : "GPU: --";
    }

    private string BuildMemoryInfo()
    {
        if (_monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue)
        {
            var usagePercent = (_monitor.MemoryUsed.Value / _monitor.MemoryTotal.Value) * 100;
            return $"{usagePercent:F0}% | {_monitor.MemoryUsed.Value:F1}/{_monitor.MemoryTotal.Value:F1} GB";
        }
        return "内存: --";
    }

    private string BuildNetworkInfo()
    {
        _stringBuilder.Clear();
        if (_monitor.NetworkDownload.HasValue)
            _stringBuilder.AppendFormat("↓ {0:F2} MB/s", _monitor.NetworkDownload.Value);

        if (_monitor.NetworkUpload.HasValue)
        {
            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(" ");
            _stringBuilder.AppendFormat("↑ {0:F2} MB/s", _monitor.NetworkUpload.Value);
        }

        return _stringBuilder.Length > 0 ? _stringBuilder.ToString() : "网络: --";
    }

    private string BuildDiskInfo()
    {
        if (_monitor.DiskFreeSpace.HasValue && _monitor.DiskTotalSpace.HasValue)
        {
            var used = _monitor.DiskTotalSpace.Value - _monitor.DiskFreeSpace.Value;
            var usagePercent = (used / _monitor.DiskTotalSpace.Value) * 100;
            return $"{usagePercent:F0}% | {used:F1}/{_monitor.DiskTotalSpace.Value:F1} GB";
        }
        return "磁盘: --";
    }

    private string BuildBatteryInfo()
    {
        if (_monitor.BatteryLevel.HasValue)
        {
            var status = _monitor.IsCharging.HasValue && _monitor.IsCharging.Value ? "⚡" : "";
            return $"{status}{_monitor.BatteryLevel.Value:F0}%";
        }
        return "电池: --";
    }

    public void ToggleGameMode()
    {
        GameMode = !GameMode;
        _settings.Performance.GameMode = GameMode;
        _settingsService.SaveSettings();
        
        _monitor.Stop();
        _monitor.Start(GameMode ? _settings.Performance.GameModeRefreshInterval : _settings.Performance.RefreshInterval);
    }

    public void UpdateSettings(Settings settings)
    {
        _settings = settings;
        _settingsService.SaveSettings();
        
        _monitor.Stop();
        _monitor.Start(_settings.Performance.GameMode ? _settings.Performance.GameModeRefreshInterval : _settings.Performance.RefreshInterval);
    }
}