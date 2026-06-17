using System;

namespace ComputerCompanion.Services;

public interface ILatencyMonitorService : IDisposable
{
    int? NetworkLatency { get; }
    
    event Action? LatencyUpdated;
    
    void Start(int intervalMs = 3000);
    void Stop();
}