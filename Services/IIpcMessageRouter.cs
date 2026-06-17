using System;

namespace ComputerCompanion.Services;

/// <summary>
/// IPC 消息路由器接口
/// 负责处理 IPC 消息的分发和处理
/// </summary>
public interface IIpcMessageRouter
{
    /// <summary>
    /// 注册消息处理器
    /// </summary>
    /// <param name="messageType">消息类型</param>
    /// <param name="handler">处理函数</param>
    void RegisterHandler(string messageType, Action<IpcMessage> handler);
    
    /// <summary>
    /// 取消注册消息处理器
    /// </summary>
    /// <param name="messageType">消息类型</param>
    void UnregisterHandler(string messageType);
    
    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    /// <param name="message">IPC 消息</param>
    void RouteMessage(IpcMessage message);
    
    /// <summary>
    /// 启动路由器（订阅 IPC 服务消息）
    /// </summary>
    void Start();
    
    /// <summary>
    /// 停止路由器（取消订阅）
    /// </summary>
    void Stop();
}