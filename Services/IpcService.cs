using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ComputerCompanion.Services;

public class IpcService : IIpcService
{
    private const string PipeName = "ComputerCompanion_IPC";
    private const int ReconnectDelayMs = 2000;
    private const int ConnectTimeoutMs = 5000;
    
    private NamedPipeServerStream? _server;
    private NamedPipeClientStream? _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isServer;
    private bool _isDisposed;
    
    public event Action<IpcMessage>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public bool IsConnected
    {
        get
        {
            if (_isServer)
                return _server?.IsConnected ?? false;
            else
                return _client?.IsConnected ?? false;
        }
    }

    public async Task StartServerAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(IpcService));

        _isServer = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        await RunServerLoopAsync(_cancellationTokenSource.Token);
    }

    private async Task RunServerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_isDisposed)
        {
            try
            {
                _server?.Dispose();
                _server = new NamedPipeServerStream(
                    PipeName, 
                    PipeDirection.InOut, 
                    1, 
                    PipeTransmissionMode.Byte, 
                    PipeOptions.Asynchronous);
                
                await _server.WaitForConnectionAsync(cancellationToken);
                OnConnected();
                
                _ = Task.Run(
                    () => ReadMessagesLoopAsync(_server, cancellationToken), 
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IPC Server error: {ex.Message}");
                await Task.Delay(ReconnectDelayMs, cancellationToken);
            }
        }
    }

    public async Task ConnectAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(IpcService));

        _isServer = false;
        _cancellationTokenSource = new CancellationTokenSource();
        
        await RunClientLoopAsync(_cancellationTokenSource.Token);
    }

    private async Task RunClientLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_isDisposed)
        {
            try
            {
                _client?.Dispose();
                _client = new NamedPipeClientStream(
                    ".", 
                    PipeName, 
                    PipeDirection.InOut, 
                    PipeOptions.Asynchronous);
                
                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectCts.CancelAfter(ConnectTimeoutMs);
                
                await _client.ConnectAsync(connectCts.Token);
                OnConnected();
                
                await ReadMessagesLoopAsync(_client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("IPC connection timeout, retrying...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IPC Client error: {ex.Message}");
            }
            
            if (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                await Task.Delay(ReconnectDelayMs, cancellationToken);
            }
        }
    }

    private async Task ReadMessagesLoopAsync(PipeStream pipeStream, CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                var bytesRead = await pipeStream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                if (bytesRead == 0)
                {
                    OnDisconnected();
                    break;
                }
                
                var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 65536)
                {
                    Console.WriteLine($"Invalid message length: {messageLength}");
                    continue;
                }
                
                var messageBuffer = new byte[messageLength];
                bytesRead = await pipeStream.ReadAsync(messageBuffer, 0, messageLength, cancellationToken);
                if (bytesRead == 0)
                {
                    OnDisconnected();
                    break;
                }
                
                await ProcessMessageAsync(messageBuffer, bytesRead);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading IPC message: {ex.Message}");
            OnDisconnected();
        }
    }

    private async Task ProcessMessageAsync(byte[] buffer, int bytesRead)
    {
        try
        {
            var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var message = JsonConvert.DeserializeObject<IpcMessage>(json);

            if (message != null && !string.IsNullOrEmpty(message.Type))
            {
                // 使用线程安全的方式触发事件，确保在正确的线程上下文中处理
                var handler = Volatile.Read(ref MessageReceived);
                if (handler != null)
                {
                    try
                    {
                        handler(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理IPC消息时发生错误: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process IPC message: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(IpcMessage message)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(IpcService));

        if (message == null || string.IsNullOrEmpty(message.Type))
            throw new ArgumentException("Invalid message");

        try
        {
            PipeStream? pipeStream = _isServer ? (PipeStream?)_server : _client;
            if (pipeStream == null || !pipeStream.IsConnected)
            {
                Console.WriteLine("IPC not connected, message dropped");
                return;
            }

            var json = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            if (bytes.Length > 65536)
            {
                Console.WriteLine("Message too large");
                return;
            }

            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            
            CancellationToken token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            
            await pipeStream.WriteAsync(lengthBytes.AsMemory(0, 4), token);
            await pipeStream.WriteAsync(bytes.AsMemory(0, bytes.Length), token);
            await pipeStream.FlushAsync(token);
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("IPC pipe disposed");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IPC IO error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send IPC message: {ex.Message}");
        }
    }

    private void OnConnected()
    {
        Console.WriteLine("IPC connected");
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected()
    {
        Console.WriteLine("IPC disconnected");
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch
        {
        }
        
        try
        {
            _server?.Dispose();
        }
        catch
        {
        }
        
        try
        {
            _client?.Dispose();
        }
        catch
        {
        }
        
        try
        {
            _cancellationTokenSource?.Dispose();
        }
        catch
        {
        }
    }
}

public class IpcMessage
{
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}

public static class IpcMessageTypes
{
    public const string SettingsChanged = "SettingsChanged";
    public const string ShowMainWindow = "ShowMainWindow";
    public const string ExitApplication = "ExitApplication";
    public const string OverlayReady = "OverlayReady";
}