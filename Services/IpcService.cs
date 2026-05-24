using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ComputerCompanion.Services;

/// <summary>
/// 进程间通信服务 - 使用命名管道
/// </summary>
public class IpcService : IDisposable
{
    private const string PipeName = "ComputerCompanion_IPC";
    private NamedPipeServerStream? _server;
    private NamedPipeClientStream? _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isServer;
    
    public event Action<IpcMessage>? MessageReceived;

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

    /// <summary>
    /// 启动服务器模式（主程序）
    /// </summary>
    public async Task StartServerAsync()
    {
        _isServer = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                _server?.Dispose();
                _server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                
                await _server.WaitForConnectionAsync(_cancellationTokenSource.Token);
                
                _ = Task.Run(async () => await ReadMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// 连接到服务器（悬浮窗进程）
    /// </summary>
    public async Task ConnectAsync()
    {
        _isServer = false;
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            _client?.Dispose();
            _client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _client.ConnectAsync(5000, _cancellationTokenSource.Token);
            
            _ = Task.Run(async () => await ReadMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessageAsync(IpcMessage message)
    {
        try
        {
            PipeStream? pipeStream = _isServer ? _server : _client;
            if (pipeStream == null || !pipeStream.IsConnected) return;

            var json = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            await pipeStream.WriteAsync(lengthBytes, 0, 4, token);
            await pipeStream.WriteAsync(bytes, 0, bytes.Length, token);
            await pipeStream.FlushAsync(token);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 读取消息循环
    /// </summary>
    private async Task ReadMessagesAsync(CancellationToken cancellationToken)
    {
        PipeStream? pipeStream = _isServer ? _server : _client;
        if (pipeStream == null) return;

        var lengthBuffer = new byte[4];
        
        while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
        {
            try
            {
                var bytesRead = await pipeStream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                if (bytesRead == 0) break;
                
                var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                var messageBuffer = new byte[messageLength];
                
                bytesRead = await pipeStream.ReadAsync(messageBuffer, 0, messageLength, cancellationToken);
                if (bytesRead == 0) break;
                
                var json = Encoding.UTF8.GetString(messageBuffer, 0, bytesRead);
                var message = JsonConvert.DeserializeObject<IpcMessage>(json);
                
                if (message != null)
                {
                    MessageReceived?.Invoke(message);
                }
            }
            catch (Exception)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _server?.Dispose();
        _client?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// IPC 消息类型
/// </summary>
public class IpcMessage
{
    public string Type { get; set; } = "";
    public string Data { get; set; } = "";
}

/// <summary>
/// 消息类型常量
/// </summary>
public static class IpcMessageTypes
{
    public const string SettingsChanged = "SettingsChanged";
    public const string ShowMainWindow = "ShowMainWindow";
    public const string ExitApplication = "ExitApplication";
    public const string OverlayReady = "OverlayReady";
}
