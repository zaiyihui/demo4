using ComputerCompanion.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComputerCompanion.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly HardwareMonitorService _monitor;

    [ObservableProperty]
    private string _cpuInfo = "CPU: --";

    [ObservableProperty]
    private string _gpuInfo = "GPU: --";

    [ObservableProperty]
    private string _memInfo = "MEM: --";

    public MainWindowViewModel()
    {
        _monitor = new HardwareMonitorService();
        _monitor.DataUpdated += OnDataUpdated;
        _monitor.Start(1000);
    }

    private void OnDataUpdated()
    {
        CpuInfo = _monitor.CpuUsage.HasValue && _monitor.CpuTemp.HasValue 
            ? $"CPU: {_monitor.CpuUsage.Value:F1}% | {_monitor.CpuTemp.Value:F0}°C" 
            : "CPU: --";
        
        GpuInfo = _monitor.GpuUsage.HasValue && _monitor.GpuTemp.HasValue 
            ? $"GPU: {_monitor.GpuUsage.Value:F1}% | {_monitor.GpuTemp.Value:F0}°C" 
            : "GPU: --";
        
        MemInfo = _monitor.MemoryUsed.HasValue && _monitor.MemoryTotal.HasValue 
            ? $"MEM: {_monitor.MemoryUsed.Value:F1}/{_monitor.MemoryTotal.Value:F1} GB" 
            : "MEM: --";
    }
}
