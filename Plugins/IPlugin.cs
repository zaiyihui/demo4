using System;

namespace ComputerCompanion.Plugins;

public interface IPlugin : IDisposable
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Version { get; }
    string Author { get; }
    
    bool IsEnabled { get; }
    
    void Initialize();
    void Start();
    void Stop();
    
    event EventHandler<PluginEventArgs>? StatusChanged;
}

public class PluginEventArgs : EventArgs
{
    public PluginStatus Status { get; }
    public string? Message { get; }
    
    public PluginEventArgs(PluginStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }
}

public enum PluginStatus
{
    Initialized,
    Started,
    Stopped,
    Error,
    Disposed
}