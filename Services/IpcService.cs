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
    private const int MaxMessageSize = 65536;
    
    private NamedPipeServerStream? _server;
    private NamedPipeClientStream? _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isServer;
    private bool _isDisposed;
    private readonly ISecurityService? _securityService;
    private string? _sessionKey;
    
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

    public IpcService() : this(null) { }

    public IpcService(ISecurityService? securityService)
    {
        _securityService = securityService;
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
                
                if (_securityService != null)
                {
                    _sessionKey = _securityService.GenerateSessionKey();
                    await SendSessionKeyAsync(_server);
                }
                
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
                Program.Log($"[IPC] Server error: {ex.Message}");
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
                
                if (_securityService != null)
                {
                    await ReceiveSessionKeyAsync(_client);
                }
                
                OnConnected();
                
                await ReadMessagesLoopAsync(_client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Program.Log("[IPC] connection timeout, retrying...");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"[IPC] Client error: {ex.Message}");
            }
            
            if (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                await Task.Delay(ReconnectDelayMs, cancellationToken);
            }
        }
    }

    private async Task SendSessionKeyAsync(PipeStream pipeStream)
    {
        if (string.IsNullOrEmpty(_sessionKey))
            return;

        try
        {
            var sessionMessage = new IpcMessage
            {
                Type = IpcMessageTypes.SessionKey,
                Data = _sessionKey
            };
            
            var json = JsonConvert.SerializeObject(sessionMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            await pipeStream.WriteAsync(lengthBytes.AsMemory(0, 4));
            await pipeStream.WriteAsync(bytes.AsMemory(0, bytes.Length));
            await pipeStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Program.Log($"[IPC] Failed to send session key: {ex.Message}");
        }
    }

    private async Task ReceiveSessionKeyAsync(PipeStream pipeStream)
    {
        var lengthBuffer = new byte[4];
        try
        {
            await pipeStream.ReadAsync(lengthBuffer, 0, 4);
            var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            
            if (messageLength <= 0 || messageLength > MaxMessageSize)
            {
                throw new InvalidOperationException("Invalid session key message length");
            }
            
            var messageBuffer = new byte[messageLength];
            await pipeStream.ReadAsync(messageBuffer, 0, messageLength);
            
            var json = Encoding.UTF8.GetString(messageBuffer);
            var message = JsonConvert.DeserializeObject<IpcMessage>(json);
            
            if (message?.Type == IpcMessageTypes.SessionKey && !string.IsNullOrEmpty(message.Data))
            {
                _sessionKey = message.Data;
                Program.Log("[IPC] Session key received");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[IPC] Failed to receive session key: {ex.Message}");
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
                if (messageLength <= 0 || messageLength > MaxMessageSize)
                {
                    Program.Log($"[IPC] Invalid message length: {messageLength}");
                    continue;
                }
                
                var messageBuffer = new byte[messageLength];
                bytesRead = await pipeStream.ReadAsync(messageBuffer, 0, messageLength, cancellationToken);
                if (bytesRead == 0)
                {
                    OnDisconnected();
                    break;
                }
                
                ProcessMessageAsync(messageBuffer, bytesRead);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[IPC] Error reading message: {ex.Message}");
            OnDisconnected();
        }
    }

    private void ProcessMessageAsync(byte[] buffer, int bytesRead)
    {
        try
        {
            var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var message = JsonConvert.DeserializeObject<SecureIpcMessage>(json);

            if (message == null || string.IsNullOrEmpty(message.Type))
            {
                Program.Log("[IPC] Invalid message received");
                return;
            }

            if (!ValidateMessageSignature(message))
            {
                Program.Log("[IPC] Message signature validation failed");
                return;
            }

            var plainMessage = new IpcMessage
            {
                Type = message.Type,
                Data = message.Data
            };

            var handler = Volatile.Read(ref MessageReceived);
            if (handler != null)
            {
                try
                {
                    handler(plainMessage);
                }
                catch (Exception ex)
                {
                    Program.Log($"[IPC] Error processing message: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[IPC] Failed to process message: {ex.Message}");
        }
    }

    private bool ValidateMessageSignature(SecureIpcMessage message)
    {
        if (_securityService == null)
            return true;

        if (string.IsNullOrEmpty(message.Signature))
        {
            Program.Log("[IPC] Message without signature");
            return false;
        }

        var dataToSign = $"{message.Type}|{message.Data}|{message.Timestamp}";
        return _securityService.VerifySignature(dataToSign, message.Signature);
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
                Program.Log("[IPC] not connected, message dropped");
                return;
            }

            var secureMessage = CreateSecureMessage(message);
            var json = JsonConvert.SerializeObject(secureMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            if (bytes.Length > MaxMessageSize)
            {
                Program.Log("[IPC] Message too large");
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
            Program.Log("[IPC] pipe disposed");
        }
        catch (IOException ex)
        {
            Program.Log($"[IPC] IO error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Program.Log($"[IPC] Failed to send message: {ex.Message}");
        }
    }

    private SecureIpcMessage CreateSecureMessage(IpcMessage message)
    {
        var secureMessage = new SecureIpcMessage
        {
            Type = message.Type,
            Data = message.Data,
            Timestamp = DateTime.UtcNow.Ticks
        };

        if (_securityService != null)
        {
            var dataToSign = $"{message.Type}|{message.Data}|{secureMessage.Timestamp}";
            secureMessage.Signature = _securityService.SignMessage(dataToSign);
        }

        return secureMessage;
    }

    private void OnConnected()
    {
        Program.Log("[IPC] connected");
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected()
    {
        Console.WriteLine("IPC disconnected");
        _sessionKey = null;
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

public class SecureIpcMessage : IpcMessage
{
    public long Timestamp { get; set; }
    public string Signature { get; set; } = string.Empty;
}

public static class IpcMessageTypes
{
    // 基础控制消息
    public const string SettingsChanged = "SettingsChanged";
    public const string ShowMainWindow = "ShowMainWindow";
    public const string ExitApplication = "ExitApplication";
    public const string OverlayReady = "OverlayReady";
    public const string SessionKey = "SessionKey";
    
    // 增强型消息 - 进程间状态同步
    public const string Heartbeat = "Heartbeat";              // 心跳检测
    public const string StatusUpdate = "StatusUpdate";         // 状态更新
    public const string HardwareData = "HardwareData";        // 硬件数据
    public const string PositionChanged = "PositionChanged";     // 位置变更
    public const string VisibilityChanged = "VisibilityChanged";  // 可见性变更
    public const string RefreshIntervalChanged = "RefreshIntervalChanged";  // 刷新频率变更
    public const string ToggleOverlay = "ToggleOverlay";       // 切换悬浮窗
    public const string Error = "Error";                        // 错误信息
}