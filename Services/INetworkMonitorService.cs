using System;

namespace ComputerCompanion.Services;

public interface INetworkMonitorService : IDisposable
{
    float? NetworkUpload { get; }
    float? NetworkDownload { get; }
    
    event Action? NetworkDataUpdated;
    
    void Start(int intervalMs = 1000);
    void Stop();
}