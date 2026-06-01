using ComputerCompanion.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ComputerCompanion.Tests;

public class IpcServiceTests : IDisposable
{
    private IIpcService? _server;
    private IIpcService? _client;
    private bool _disposed = false;

    [Fact]
    public void Constructor_InitializesService()
    {
        var service = new IpcService();
        Assert.NotNull(service);
        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task Server_CanStart()
    {
        _server = new IpcService();
        var exception = await Record.ExceptionAsync(() => _server.StartServerAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task Client_CanConnect()
    {
        _server = new IpcService();
        _client = new IpcService();

        var serverTask = _server.StartServerAsync();
        await Task.Delay(100);

        var connectTask = _client.ConnectAsync();
        var connected = await Task.WhenAny(connectTask, Task.Delay(5000));
        
        Assert.Same(connectTask, connected);
        Assert.True(_client.IsConnected);
    }

    [Fact]
    public async Task SendMessage_WhenConnected_MessageReceived()
    {
        _server = new IpcService();
        _client = new IpcService();

        IpcMessage receivedMessage = null;
        _server.MessageReceived += msg => receivedMessage = msg;

        var serverTask = _server.StartServerAsync();
        await Task.Delay(100);
        
        await _client.ConnectAsync();
        await Task.Delay(100);

        var testMessage = new IpcMessage { Type = "Test", Data = "TestData" };
        await _client.SendMessageAsync(testMessage);
        
        await Task.Delay(200);
        
        Assert.NotNull(receivedMessage);
        Assert.Equal("Test", receivedMessage.Type);
        Assert.Equal("TestData", receivedMessage.Data);
    }

    [Fact]
    public void SendMessage_WithNullMessage_ThrowsArgumentException()
    {
        var service = new IpcService();
        Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync(null));
    }

    [Fact]
    public void SendMessage_WithEmptyType_ThrowsArgumentException()
    {
        var service = new IpcService();
        var message = new IpcMessage { Type = "", Data = "Test" };
        Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync(message));
    }

    [Fact]
    public void Dispose_StopsService()
    {
        _server = new IpcService();
        _server.Dispose();
        
        Assert.False(_server.IsConnected);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var service = new IpcService();
        service.Dispose();
        
        var exception = Record.Exception(() => service.Dispose());
        Assert.Null(exception);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _client?.Dispose();
            _server?.Dispose();
        }
        
        _disposed = true;
    }
}