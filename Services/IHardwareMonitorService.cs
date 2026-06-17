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
    
    float? DiskFreeSpace { get; }
    float? DiskTotalSpace { get; }
    
    float? Fps { get; }
    
    bool HasGpu { get; }
    
    event Action? DataUpdated;
    
    void Start(int intervalMs = 1000);
    void Stop();
    void MarkFrame();
    float? GetSmoothedFps();
}