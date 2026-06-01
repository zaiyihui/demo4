using System;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

public interface IIpcService : IDisposable
{
    bool IsConnected { get; }
    
    event Action<IpcMessage>? MessageReceived;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    
    Task StartServerAsync();
    Task ConnectAsync();
    Task SendMessageAsync(IpcMessage message);
}