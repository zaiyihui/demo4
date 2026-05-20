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
    private string _memoryInfo = "MEM: --";

    [ObservableProperty]
    private string _networkInfo = "NET: --";

    [ObservableProperty]
    private string _diskInfo = "DISK: --";

    [ObservableProperty]
    private string _batteryInfo = "BAT: --";

    [ObservableProperty]
    private bool _showGpu = true;

    [ObservableProperty]
    private bool _showBattery = true;

    [ObservableProperty]
    private bool _gameMode = false;

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

    private void OnDataUpdated()
    {
        if (_settings.ShowCpu)
        {
            CpuInfo = BuildCpuInfo();
        }

        if (_settings.ShowGpu && _monitor.HasGpu)
        {
            GpuInfo = BuildGpuInfo();
        }

        if (_settings.ShowMemory)
        {
            MemoryInfo = BuildMemoryInfo();
        }

        if (_settings.ShowNetwork)
        {
            NetworkInfo = BuildNetworkInfo();
        }

        if (_settings.ShowDisk)
        {
            DiskInfo = BuildDiskInfo();
        }

        if (_settings.ShowBattery && _monitor.HasBattery)
        {
            BatteryInfo = BuildBatteryInfo();
        }
    }

    private string BuildCpuInfo()
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if (_monitor.CpuUsage.HasValue)
            parts.Add($"CPU {_monitor.CpuUsage.Value:F1}%");
        
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
            parts.Add($"GPU {_monitor.GpuUsage.Value:F1}%");
        
        if (_monitor.GpuTemp.HasValue)
            parts.Add($"{_monitor.GpuTemp.Value:F0}°C");
        
        if (_monitor.GpuVramUsed.HasValue && _monitor.GpuVramTotal.HasValue)
            parts.Add($"VRAM {_monitor.GpuVramUsed.Value:F1}/{_monitor.GpuVramTotal.Value:F1} GB");
        
        if (_monitor.GpuFanSpeed.HasValue && _monitor.GpuFanSpeed.Value > 0)
            parts.Add($"{_monitor.GpuFanSpeed.Value} RPM");
        
        return string.Join(" | ", parts) ?? "GPU: --";
    }

    private string BuildMemoryInfo()
    {
        if (_monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue)
        {
            var usagePercent = (_monitor.MemoryUsed.Value / _monitor.MemoryTotal.Value) * 100;
            return $"MEM {usagePercent:F0}% | {_monitor.MemoryUsed.Value:F1}/{_monitor.MemoryTotal.Value:F1} GB";
        }
        return "MEM: --";
    }

    private string BuildNetworkInfo()
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if (_monitor.NetworkDownload.HasValue)
            parts.Add($"↓ {_monitor.NetworkDownload.Value:F2} MB/s");
        
        if (_monitor.NetworkUpload.HasValue)
            parts.Add($"↑ {_monitor.NetworkUpload.Value:F2} MB/s");
        
        return string.Join(" ", parts) ?? "NET: --";
    }

    private string BuildDiskInfo()
    {
        if (_monitor.DiskFreeSpace.HasValue && _monitor.DiskTotalSpace.HasValue)
        {
            var used = _monitor.DiskTotalSpace.Value - _monitor.DiskFreeSpace.Value;
            var usagePercent = (used / _monitor.DiskTotalSpace.Value) * 100;
            return $"DISK {usagePercent:F0}% | {used:F1}/{_monitor.DiskTotalSpace.Value:F1} GB";
        }
        return "DISK: --";
    }

    private string BuildBatteryInfo()
    {
        if (_monitor.BatteryLevel.HasValue)
        {
            var status = _monitor.IsCharging.HasValue && _monitor.IsCharging.Value ? "⚡" : "";
            return $"BAT {status}{_monitor.BatteryLevel.Value:F0}%";
        }
        return "BAT: --";
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
