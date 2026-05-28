using ComputerCompanion.Models;
using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComputerCompanion.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly HardwareMonitorService _monitor;
    private readonly SettingsService _settingsService;
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

    public MainWindowViewModel()
    {
        _settingsService = new SettingsService();
        _settings = _settingsService.GetSettings();
        
        _monitor = new HardwareMonitorService();
        _monitor.DataUpdated += OnDataUpdated;
        _monitor.Start(_settings.GameMode ? _settings.GameModeRefreshInterval : _settings.RefreshInterval);
        
        ShowGpu = _monitor.HasGpu && _settings.ShowGpu;
        ShowBattery = _monitor.HasBattery && _settings.ShowBattery;
    }
    
    public MainWindowViewModel(HardwareMonitorService monitor, Settings settings)
    {
        _settings = settings;
        _settingsService = App.SettingsService ?? new SettingsService();
        _monitor = monitor;
        _monitor.DataUpdated += OnDataUpdated;
        
        ShowGpu = _monitor.HasGpu && _settings.ShowGpu;
        ShowBattery = _monitor.HasBattery && _settings.ShowBattery;
    }

    private void OnDataUpdated()
    {
        if (_settings.ShowCpu)
        {
            CpuInfo = BuildCpuInfo();
            CpuUsagePercent = _monitor.CpuUsage ?? 0;
        }

        if (_settings.ShowGpu && _monitor.HasGpu)
        {
            GpuInfo = BuildGpuInfo();
            GpuUsagePercent = _monitor.GpuUsage ?? 0;
        }

        if (_settings.ShowMemory)
        {
            MemoryInfo = BuildMemoryInfo();
            if (_monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue)
            {
                MemoryUsagePercent = (_monitor.MemoryUsed.Value / _monitor.MemoryTotal.Value) * 100;
            }
        }

        if (_settings.ShowNetwork)
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

        if (_settings.ShowDisk)
        {
            DiskInfo = BuildDiskInfo();
            if (_monitor.DiskFreeSpace.HasValue && _monitor.DiskTotalSpace.HasValue)
            {
                var used = _monitor.DiskTotalSpace.Value - _monitor.DiskFreeSpace.Value;
                DiskUsagePercent = (used / _monitor.DiskTotalSpace.Value) * 100;
            }
        }

        if (_settings.ShowBattery && _monitor.HasBattery)
        {
            BatteryInfo = BuildBatteryInfo();
            BatteryLevelPercent = _monitor.BatteryLevel ?? 0;
        }
    }

    private string BuildCpuInfo()
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if (_monitor.CpuUsage.HasValue)
            parts.Add($"{_monitor.CpuUsage.Value:F1}%");
        
        if (_monitor.CpuTemp.HasValue)
            parts.Add($"{_monitor.CpuTemp.Value:F0}°C");
        
        if (_monitor.CpuFanSpeed.HasValue && _monitor.CpuFanSpeed.Value > 0)
            parts.Add($"{_monitor.CpuFanSpeed.Value} RPM");
        
        return string.Join(" | ", parts) ?? "CPU: --";
    }

    private string BuildGpuInfo()
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if (_monitor.GpuUsage.HasValue)
            parts.Add($"{_monitor.GpuUsage.Value:F1}%");
        
        if (_monitor.GpuTemp.HasValue)
            parts.Add($"{_monitor.GpuTemp.Value:F0}°C");
        
        if (_monitor.GpuVramUsed.HasValue && _monitor.GpuVramTotal.HasValue)
            parts.Add($"{_monitor.GpuVramUsed.Value:F1}/{_monitor.GpuVramTotal.Value:F1} GB");
        
        if (_monitor.GpuFanSpeed.HasValue && _monitor.GpuFanSpeed.Value > 0)
            parts.Add($"{_monitor.GpuFanSpeed.Value} RPM");
        
        return string.Join(" | ", parts) ?? "GPU: --";
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
        var parts = new System.Collections.Generic.List<string>();
        
        if (_monitor.NetworkDownload.HasValue)
            parts.Add($"↓ {_monitor.NetworkDownload.Value:F2} MB/s");
        
        if (_monitor.NetworkUpload.HasValue)
            parts.Add($"↑ {_monitor.NetworkUpload.Value:F2} MB/s");
        
        return string.Join(" ", parts) ?? "网络: --";
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
        _settings.GameMode = GameMode;
        _settingsService.SaveSettings();
        
        _monitor.Stop();
        _monitor.Start(GameMode ? _settings.GameModeRefreshInterval : _settings.RefreshInterval);
    }

    public void UpdateSettings(Settings settings)
    {
        _settings = settings;
        _settingsService.SaveSettings();
        
        _monitor.Stop();
        _monitor.Start(_settings.GameMode ? _settings.GameModeRefreshInterval : _settings.RefreshInterval);
    }
}
