using System;

namespace ComputerCompanion.Services;

public interface IHardwareMonitorService : IDisposable
{
    float? CpuUsage { get; }
    float? CpuTemp { get; }
    int? CpuFanSpeed { get; }
    
    float? GpuUsage { get; }
    float? GpuTemp { get; }
    int? GpuFanSpeed { get; }
    float? GpuVramUsed { get; }
    float? GpuVramTotal { get; }
    float? GpuClock { get; }
    
    float? MemoryUsed { get; }
    float? MemoryTotal { get; }
    
    float? NetworkUpload { get; }
    float? NetworkDownload { get; }
    
    float? DiskFreeSpace { get; }
    float? DiskTotalSpace { get; }
    
    float? BatteryLevel { get; }
    bool? IsCharging { get; }
    
    bool HasGpu { get; }
    bool HasBattery { get; }
    
    float? Fps { get; }
    int? NetworkLatency { get; }
    
    event Action? DataUpdated;
    
    void Start(int intervalMs = 1000);
    void Stop();
    void MarkFrame();
    float? GetSmoothedFps();
}