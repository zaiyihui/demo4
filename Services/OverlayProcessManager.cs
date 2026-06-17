using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

/// <summary>
/// 悬浮窗进程管理器实现
/// 从 App.axaml.cs 中抽取的悬浮窗进程管理逻辑
/// </summary>
public class OverlayProcessManager : IOverlayProcessManager
{
    #region 私有字段
    
    private Process? _overlayProcess;
    private bool _autoRecover = true;
    private DateTime _lastStartAttempt = DateTime.MinValue;
    private int _consecutiveFailureCount = 0;
    private bool _isDisposed;
    
    // 配置常量
    private const int RestartCooldownMs = 5000;      // 重启冷却时间
    private const int MaxConsecutiveFailures = 3;    // 最大连续失败次数
    private const int GracefulShutdownTimeoutMs = 2000; // 优雅关闭等待时间
    private const int AutoRecoverDelayMs = 2000;     // 自动恢复延迟
    
    // IPC 服务引用（用于发送退出消息）
    private readonly IIpcService? _ipcService;
    
    #endregion
    
    #region 公共属性和事件
    
    public bool IsRunning
    {
        get
        {
            try
            {
                return _overlayProcess != null && !_overlayProcess.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
    
    public event EventHandler<OverlayProcessExitedEventArgs>? ProcessExited;
    
    #endregion
    
    #region 构造函数
    
    public OverlayProcessManager() : this(null) { }
    
    public OverlayProcessManager(IIpcService? ipcService)
    {
        _ipcService = ipcService;
    }
    
    #endregion
    
    #region 公共方法
    
    public void Start()
    {
        if (_isDisposed)
        {
            Program.Log("[悬浮窗管理] 已 disposed，无法启动");
            return;
        }
        
        // 防止过快重启
        if ((DateTime.Now - _lastStartAttempt).TotalMilliseconds < RestartCooldownMs)
        {
            Program.Log("[悬浮窗管理] 距离上次启动时间过短，跳过启动");
            return;
        }
        
        // 检查是否已在运行
        if (IsRunning)
        {
            Program.Log("[悬浮窗管理] 悬浮窗进程已在运行");
            return;
        }
        
        // 检查连续失败次数
        if (_consecutiveFailureCount >= MaxConsecutiveFailures)
        {
            Program.Log($"[悬浮窗管理] 已连续失败 {_consecutiveFailureCount} 次，停止自动恢复");
            return;
        }
        
        _lastStartAttempt = DateTime.Now;
        
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("无法获取应用程序路径");
            }
            
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = Program.OverlayModeArg,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? "."
            };
            
            _overlayProcess = Process.Start(psi);
            
            if (_overlayProcess != null)
            {
                _overlayProcess.EnableRaisingEvents = true;
                _overlayProcess.Exited += OnProcessExited;
                
                Program.Log($"[悬浮窗管理] 悬浮窗进程已启动 PID={_overlayProcess.Id}");
                _consecutiveFailureCount = 0; // 重置失败计数
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[悬浮窗管理] 启动悬浮窗进程失败: {ex.Message}");
            _consecutiveFailureCount++;
        }
    }
    
    public void Stop()
    {
        if (_isDisposed)
            return;
        
        _autoRecover = false; // 手动停止时禁用自动恢复
        
        try
        {
            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                // 先尝试通过 IPC 发送退出消息
                if (_ipcService != null && _ipcService.IsConnected)
                {
                    _ = App.SendIpcMessageAsync(_ipcService, IpcMessageTypes.ExitApplication);
                }
                
                // 等待进程退出
                if (!_overlayProcess.WaitForExit(GracefulShutdownTimeoutMs))
                {
                    Program.Log("[悬浮窗管理] 悬浮窗未在指定时间内退出，强制终止");
                    _overlayProcess.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[悬浮窗管理] 停止悬浮窗进程失败: {ex.Message}");
        }
        finally
        {
            CleanupProcess();
            _consecutiveFailureCount = 0;
            _autoRecover = true; // 重新启用自动恢复
        }
    }
    
    public void Restart()
    {
        if (_isDisposed)
            return;
        
        Program.Log("[悬浮窗管理] 正在重启悬浮窗进程...");
        Stop();
        Task.Delay(500).Wait(); // 短暂延迟确保进程完全退出
        Start();
    }
    
    public void SetAutoRecover(bool enabled)
    {
        _autoRecover = enabled;
        Program.Log($"[悬浮窗管理] 自动恢复已设置为: {enabled}");
    }
    
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        _autoRecover = false;
        
        CleanupProcess();
        
        GC.SuppressFinalize(this);
        Program.Log("[悬浮窗管理] 已释放");
    }
    
    #endregion
    
    #region 私有方法
    
    private void OnProcessExited(object? sender, EventArgs e)
    {
        try
        {
            var process = sender as Process;
            var exitCode = process?.ExitCode ?? -1;
            
            Program.Log($"[悬浮窗管理] 悬浮窗进程已退出，退出码={exitCode}");
            
            // 触发事件
            var args = new OverlayProcessExitedEventArgs
            {
                ExitCode = exitCode,
                IsUnexpectedExit = exitCode != 0
            };
            ProcessExited?.Invoke(this, args);
            
            CleanupProcess();
        }
        catch (Exception ex)
        {
            Program.Log($"[悬浮窗管理] 处理进程退出事件失败: {ex.Message}");
        }
    }
    
    private void CleanupProcess()
    {
        try
        {
            if (_overlayProcess != null)
            {
                _overlayProcess.Exited -= OnProcessExited;
                _overlayProcess.Dispose();
            }
        }
        catch { }
        
        _overlayProcess = null;
    }
    
    /// <summary>
    /// 尝试自动恢复悬浮窗进程（由外部调用，如设置服务检测到启用悬浮窗）
    /// </summary>
    public void TryAutoRecover()
    {
        if (_isDisposed || !_autoRecover || IsRunning)
            return;
        
        Program.Log("[悬浮窗管理] 尝试自动恢复悬浮窗进程...");
        _ = Task.Delay(AutoRecoverDelayMs).ContinueWith(_ => Start());
    }
    
    #endregion
}