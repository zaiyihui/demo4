using System;

namespace ComputerCompanion.Services;

/// <summary>
/// 悬浮窗进程管理器接口
/// 负责悬浮窗子进程的启动、停止、重启和自动恢复
/// </summary>
public interface IOverlayProcessManager : IDisposable
{
    /// <summary>
    /// 悬浮窗进程是否正在运行
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 悬浮窗进程退出事件
    /// </summary>
    event EventHandler<OverlayProcessExitedEventArgs>? ProcessExited;
    
    /// <summary>
    /// 启动悬浮窗进程
    /// </summary>
    void Start();
    
    /// <summary>
    /// 停止悬浮窗进程
    /// </summary>
    void Stop();
    
    /// <summary>
    /// 重启悬浮窗进程
    /// </summary>
    void Restart();
    
    /// <summary>
    /// 启用或禁用自动恢复功能
    /// </summary>
    void SetAutoRecover(bool enabled);
    
    /// <summary>
    /// 尝试自动恢复悬浮窗进程
    /// </summary>
    void TryAutoRecover();
}

/// <summary>
/// 悬浮窗进程退出事件参数
/// </summary>
public class OverlayProcessExitedEventArgs : EventArgs
{
    /// <summary>
    /// 进程退出码
    /// </summary>
    public int ExitCode { get; set; }
    
    /// <summary>
    /// 是否为异常退出
    /// </summary>
    public bool IsUnexpectedExit { get; set; }
}