using System;
using System.Collections.Generic;

namespace ComputerCompanion.Services;

/// <summary>
/// IPC 消息路由器实现
/// 使用字典驱动的方式替代 switch-case，提供更好的可扩展性
/// </summary>
public class IpcMessageRouter : IIpcMessageRouter
{
    #region 私有字段
    
    private readonly IIpcService _ipcService;
    private readonly Dictionary<string, Action<IpcMessage>> _handlers;
    private readonly Action<IpcMessage>? _defaultHandler;
    private bool _isRunning;
    
    #endregion
    
    #region 构造函数
    
    public IpcMessageRouter(IIpcService ipcService, Action<IpcMessage>? defaultHandler = null)
    {
        _ipcService = ipcService ?? throw new ArgumentNullException(nameof(ipcService));
        _handlers = new Dictionary<string, Action<IpcMessage>>();
        _defaultHandler = defaultHandler;
    }
    
    #endregion
    
    #region 公共方法
    
    public void RegisterHandler(string messageType, Action<IpcMessage> handler)
    {
        if (string.IsNullOrEmpty(messageType))
            throw new ArgumentException("消息类型不能为空", nameof(messageType));
        
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        
        _handlers[messageType] = handler;
        Program.Log($"[IPC路由] 注册处理器: {messageType}");
    }
    
    public void UnregisterHandler(string messageType)
    {
        if (_handlers.Remove(messageType))
        {
            Program.Log($"[IPC路由] 取消注册处理器: {messageType}");
        }
    }
    
    public void RouteMessage(IpcMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Type))
        {
            Program.Log("[IPC路由] 收到无效消息");
            return;
        }
        
        Program.Log($"[IPC路由] 收到消息: {message.Type}");
        
        try
        {
            if (_handlers.TryGetValue(message.Type, out var handler))
            {
                handler(message);
            }
            else if (_defaultHandler != null)
            {
                _defaultHandler(message);
            }
            else
            {
                Program.Log($"[IPC路由] 未找到处理器: {message.Type}");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[IPC路由] 处理消息异常: {message.Type}, {ex.Message}");
        }
    }
    
    public void Start()
    {
        if (_isRunning)
            return;
        
        _ipcService.MessageReceived += OnMessageReceived;
        _isRunning = true;
        Program.Log("[IPC路由] 已启动");
    }
    
    public void Stop()
    {
        if (!_isRunning)
            return;
        
        _ipcService.MessageReceived -= OnMessageReceived;
        _isRunning = false;
        Program.Log("[IPC路由] 已停止");
    }
    
    #endregion
    
    #region 私有方法
    
    private void OnMessageReceived(IpcMessage message)
    {
        RouteMessage(message);
    }
    
    #endregion
}