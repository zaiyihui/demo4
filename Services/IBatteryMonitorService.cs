using System;

namespace ComputerCompanion.Services;

public interface IBatteryMonitorService : IDisposable
{
    float? BatteryLevel { get; }
    bool? IsCharging { get; }
    bool HasBattery { get; }
    
    event Action? BatteryUpdated;
    
    void Start(int intervalMs = 2000);
    void Stop();
}