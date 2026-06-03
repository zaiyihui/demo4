using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ComputerCompanion.Models;
using ComputerCompanion.ViewModels;
using ComputerCompanion.Views;
using ComputerCompanion.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ComputerCompanion;

public partial class App : Application
{
    public static bool IsOverlayMode { get; set; } = false;

    private static IServiceProvider? _serviceProvider;
    private static Process? _overlayProcess;
    private static MainWindow? _mainWindow;
    private static IClassicDesktopStyleApplicationLifetime? _desktopLifetime;

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("服务容器未初始化");
            return _serviceProvider;
        }
    }

    /// <summary>
    /// 检查悬浮窗进程是否正在运行
    /// </summary>
    public static bool IsOverlayRunning
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

    public override void Initialize()
    {
        try
        {
            Program.Log("[应用] Initialize 开始");
            AvaloniaXamlLoader.Load(this);
            Program.Log("[应用] XAML 加载完成");
            ConfigureServices();
            Program.Log("[应用] 服务配置完成");
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] Initialize 失败: {ex.GetType().Name}: {ex.Message}");
            Program.Log(ex.StackTrace ?? "无堆栈信息");
        }
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDataStorageService, DataStorageService>();
        services.AddSingleton<IIpcService>(sp =>
            new IpcService(sp.GetService<ISecurityService>()));
        services.AddSingleton<TrayIconService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Program.Log("[应用] OnFrameworkInitializationCompleted 开始");

        try
        {
            Program.Log("[应用] 获取设置服务");
            var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
            
            Program.Log("[应用] 获取设置对象");
            var settings = settingsService.GetSettings();
            if (settings == null)
            {
                Program.Log("[应用] 设置对象为 null，使用默认设置");
                settings = new Models.Settings();
            }

            Program.Log("[应用] 初始化数据存储服务");
            var dataStorageService = ServiceProvider.GetRequiredService<IDataStorageService>();
            var dataPath = dataStorageService.GetDataPath();
            dataStorageService.CreateDirectoryIfNotExists(dataPath);
            dataStorageService.CreateDirectoryIfNotExists(dataStorageService.GetLogPath());
            dataStorageService.CreateDirectoryIfNotExists(dataStorageService.GetCachePath());
            
            Program.Log($"[应用] 数据存储路径: {dataPath}");
            
            var settingsPath = dataStorageService.GetSettingsPath();
            settingsService.UpdateSettingsPath(settingsPath);
            Program.Log($"[应用] 设置文件路径: {settingsPath}");
            
            Program.UpdateLogPath(dataStorageService.GetLogPath());

            if (IsOverlayMode)
            {
                Program.Log("[应用] 悬浮窗模式");
                InitializeOverlayMode(settings);
            }
            else
            {
                Program.Log("[应用] 主窗口模式");
                InitializeMainMode(settings);
            }

            // 延迟启动硬件监控和其他服务（避免阻塞窗口显示）
            _ = Task.Run(() =>
            {
                try
                {
                    Program.Log("[应用] 后台启动硬件监控");
                    var hardwareMonitorService = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
                    hardwareMonitorService.Start();
                }
                catch (Exception ex)
                {
                    Program.Log($"[应用] 硬件监控后台启动失败: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 初始化失败: {ex.GetType().Name}: {ex.Message}");
            Program.Log(ex.StackTrace ?? "无堆栈信息");
        }

        base.OnFrameworkInitializationCompleted();
        Program.Log("[应用] OnFrameworkInitializationCompleted 完成");
    }

    #region 初始化方法

    private void InitializeOverlayMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktopLifetime = desktop;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var monitor = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
            var overlayWindow = new OverlayWindow();
            overlayWindow.Initialize(new OverlayViewModel(monitor, settings));
            desktop.MainWindow = overlayWindow;

            var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
            ipcService.MessageReceived += OnIpcMessageReceived;
            _ = ConnectIpcAsync(ipcService);
            _ = SendIpcMessageAsync(ipcService, IpcMessageTypes.OverlayReady, "悬浮窗已启动");

            Program.Log("[应用] 悬浮窗窗口已创建");
        }
    }

    private void InitializeMainMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                _desktopLifetime = desktop;
                
                Program.Log("[应用] 获取硬件监控服务");
                var monitor = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
                if (monitor == null)
                {
                    Program.Log("[应用] 硬件监控服务为 null");
                    throw new InvalidOperationException("硬件监控服务为 null");
                }
                
                Program.Log("[应用] 获取设置服务");
                var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
                if (settingsService == null)
                {
                    Program.Log("[应用] 设置服务为 null");
                    throw new InvalidOperationException("设置服务为 null");
                }

                Program.Log("[应用] 创建主窗口视图模型");
                var viewModel = new MainWindowViewModel(monitor, settings, settingsService);
                
                Program.Log("[应用] 创建主窗口");
                _mainWindow = new MainWindow
                {
                    DataContext = viewModel
                };

                desktop.MainWindow = _mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // 先显示窗口，再注册关闭拦截
                // 确保窗口一定可见
                _mainWindow.Show();
                _mainWindow.Activate();
                Program.Log("[应用] 主窗口已显示");

                // 关闭时隐藏而非退出
                _mainWindow.Closing += (s, e) =>
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                    Program.Log("[应用] 主窗口关闭按钮 -> 隐藏到托盘");
                };

                // IPC 服务
                var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
                ipcService.MessageReceived += OnIpcMessageReceived;
                _ = StartIpcServerAsync(ipcService);

                // 托盘图标
                try
                {
                    var trayIconService = ServiceProvider.GetRequiredService<TrayIconService>();
                    trayIconService.ShowMainWindow += (s, e) => ShowMainWindow();
                    trayIconService.ExitApplication += (s, e) => ExitApplication();
                    trayIconService.Initialize();
                    Program.Log("[应用] 托盘图标服务已初始化");
                }
                catch (Exception ex)
                {
                    Program.Log($"[应用] 托盘初始化失败（忽略）: {ex.Message}");
                }

                // 悬浮窗
                if (settings.Overlay.EnableOverlay)
                {
                    Program.Log("[应用] 配置启用悬浮窗，启动悬浮窗进程");
                    StartOverlayProcess();
                }

                // 最小化启动
                if (settings.Startup.StartMinimized)
                {
                    Program.Log("[应用] 设置为最小化启动 -> 隐藏主窗口");
                    _mainWindow.Hide();
                }

                Program.Log("[应用] 主窗口模式初始化完成");
            }
            catch (Exception ex)
            {
                Program.Log($"[应用] 主窗口初始化失败: {ex.GetType().Name}: {ex.Message}");
                Program.Log(ex.StackTrace ?? "无堆栈信息");
                // 尝试显示一个简单的错误窗口
                try
                {
                    _mainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(
                            new HardwareMonitorService(), 
                            settings, 
                            new SettingsService())
                    };
                    desktop.MainWindow = _mainWindow;
                    _mainWindow.Show();
                }
                catch { }
            }
        }
        else
        {
            Program.Log("[应用] 警告: 无法获取 IClassicDesktopStyleApplicationLifetime");
        }
    }

    #endregion

    #region IPC 消息处理

    private void OnIpcMessageReceived(IpcMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Type))
            return;

        Program.Log($"[应用] 收到IPC消息: {message.Type}");

        switch (message.Type)
        {
            case IpcMessageTypes.SettingsChanged:
                HandleSettingsChanged();
                break;
            case IpcMessageTypes.ShowMainWindow:
                HandleShowMainWindow();
                break;
            case IpcMessageTypes.ExitApplication:
                HandleExitApplication();
                break;
            case IpcMessageTypes.OverlayReady:
                HandleOverlayReady();
                break;
            case IpcMessageTypes.Heartbeat:
                Program.Log("[应用] 收到悬浮窗心跳");
                break;
            case IpcMessageTypes.StatusUpdate:
                Program.Log($"[应用] 收到状态更新: {message.Data}");
                break;
            case IpcMessageTypes.ToggleOverlay:
                HandleToggleOverlay();
                break;
            case IpcMessageTypes.Error:
                Program.Log($"[应用] 收到错误消息: {message.Data}");
                break;
        }
    }

    private void HandleSettingsChanged()
    {
        if (IsOverlayMode)
        {
            var settingsService = ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            settingsService?.LoadSettings();
        }
    }

    private void HandleShowMainWindow()
    {
        if (!IsOverlayMode) ShowMainWindow();
    }

    private void HandleExitApplication()
    {
        if (IsOverlayMode) Environment.Exit(0);
    }

    private void HandleOverlayReady()
    {
        Program.Log("[应用] 悬浮窗已成功启动并准备就绪");
        if (!IsOverlayMode)
        {
            var settingsService = ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null)
            {
                var settings = settingsService.GetSettings();
                Program.Log($"[应用] 悬浮窗配置: FPS={settings.Overlay.OverlayShowFPS}, GPU={settings.Overlay.OverlayShowGpu}");
            }
        }
    }

    private void HandleToggleOverlay()
    {
        if (IsOverlayMode) return;

        if (_overlayProcess != null && !_overlayProcess.HasExited)
        {
            StopOverlayProcess();
        }
        else
        {
            StartOverlayProcess();
        }
    }

    public static async Task SendIpcMessageAsync(IIpcService ipcService, string type, string data = "")
    {
        try
        {
            await ipcService.SendMessageAsync(new IpcMessage { Type = type, Data = data });
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 发送IPC消息失败: {ex.Message}");
        }
    }

    #endregion

    #region 悬浮窗进程管理

    // 悬浮窗进程自动恢复相关字段
    private static bool _autoRecoverOverlay = true;
    private static DateTime _lastOverlayStartAttempt = DateTime.MinValue;
    private const int OverlayRestartCooldownMs = 5000; // 5秒冷却时间，避免无限重启
    private static int _consecutiveFailureCount = 0;
    private const int MaxConsecutiveFailures = 3; // 最多连续失败3次后停止自动恢复

    public static void StartOverlayProcess()
    {
        try
        {
            // 防止过快重启
            if ((DateTime.Now - _lastOverlayStartAttempt).TotalMilliseconds < OverlayRestartCooldownMs)
            {
                Program.Log("[应用] 距离上次启动悬浮窗时间过短，跳过启动");
                return;
            }

            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                Program.Log("[应用] 悬浮窗进程已在运行");
                return;
            }

            // 检查连续失败次数
            if (_consecutiveFailureCount >= MaxConsecutiveFailures)
            {
                Program.Log($"[应用] 悬浮窗已连续失败 {_consecutiveFailureCount} 次，停止自动恢复。请检查系统环境后手动启动。");
                return;
            }

            _lastOverlayStartAttempt = DateTime.Now;

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
                WorkingDirectory = System.IO.Path.GetDirectoryName(exePath) ?? "."
            };

            _overlayProcess = Process.Start(psi);
            
            if (_overlayProcess != null)
            {
                // 启用进程退出检测，用于自动恢复
                _overlayProcess.EnableRaisingEvents = true;
                _overlayProcess.Exited += OnOverlayProcessExited;
                
                Program.Log($"[应用] 悬浮窗进程已启动 PID={_overlayProcess.Id}");
                _consecutiveFailureCount = 0; // 重置失败计数
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 启动悬浮窗进程失败: {ex.Message}");
            _consecutiveFailureCount++;
        }
    }

    private static void OnOverlayProcessExited(object? sender, EventArgs e)
    {
        try
        {
            var process = sender as Process;
            var exitCode = process?.ExitCode ?? -1;
            
            Program.Log($"[应用] 悬浮窗进程已退出，退出码={exitCode}");
            
            if (process != null)
            {
                process.Exited -= OnOverlayProcessExited;
                process.Dispose();
            }
            
            // 只有在主程序模式下才自动恢复
            if (_autoRecoverOverlay && !IsOverlayMode)
            {
                // 检查设置中是否启用了悬浮窗
                var settingsService = _serviceProvider?.GetService<ISettingsService>();
                if (settingsService?.GetSettings()?.Overlay.EnableOverlay == true)
                {
                    Program.Log("[应用] 尝试自动恢复悬浮窗进程...");
                    // 延迟一点再启动，避免立即重启
                    _ = Task.Delay(2000).ContinueWith(_ => StartOverlayProcess());
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 处理悬浮窗退出事件失败: {ex.Message}");
        }
        finally
        {
            _overlayProcess = null;
        }
    }

    public static void StopOverlayProcess()
    {
        try
        {
            _autoRecoverOverlay = false; // 手动停止时禁用自动恢复
            
            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                var ipcService = _serviceProvider?.GetService<IIpcService>();
                if (ipcService != null)
                {
                    _ = SendIpcMessageAsync(ipcService, IpcMessageTypes.ExitApplication);
                }

                if (!_overlayProcess.WaitForExit(2000))
                {
                    Program.Log("[应用] 悬浮窗未在2秒内退出，强制终止");
                    _overlayProcess.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 停止悬浮窗进程失败: {ex.Message}");
        }
        finally
        {
            _overlayProcess?.Dispose();
            _overlayProcess = null;
            _consecutiveFailureCount = 0; // 重置失败计数
            _autoRecoverOverlay = true; // 重新启用自动恢复
        }
    }

    public static void RestartOverlayProcess()
    {
        Program.Log("[应用] 正在重启悬浮窗进程...");
        StopOverlayProcess();
        Task.Delay(500).Wait(); // 短暂延迟确保进程完全退出
        StartOverlayProcess();
    }

    #endregion

    #region 窗口管理

    public static MainWindow? MainWindow => _mainWindow;

    public static void ShowMainWindow()
    {
        try
        {
            if (_mainWindow == null)
            {
                Program.Log("[应用] ShowMainWindow: _mainWindow 为空");
                return;
            }

            Program.Log("[应用] 显示主窗口");
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.WindowState = WindowState.Normal;
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] ShowMainWindow 失败: {ex.Message}");
        }
    }

    public static void ExitApplication()
    {
        Program.Log("[应用] 正在退出程序");

        StopOverlayProcess();

        try
        {
            var trayIconService = _serviceProvider?.GetService<TrayIconService>();
            var ipcService = _serviceProvider?.GetService<IIpcService>();
            var hardwareMonitorService = _serviceProvider?.GetService<IHardwareMonitorService>();

            trayIconService?.Dispose();
            ipcService?.Dispose();
            hardwareMonitorService?.Dispose();
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 资源释放异常: {ex.Message}");
        }

        try
        {
            if (_desktopLifetime != null)
            {
                _desktopLifetime.Shutdown();
                return;
            }
        }
        catch { }

        Environment.Exit(0);
    }

    #endregion

    #region 辅助方法

    private static async Task ConnectIpcAsync(IIpcService ipcService)
    {
        try
        {
            await ipcService.ConnectAsync();
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] IPC连接失败: {ex.Message}");
        }
    }

    private static async Task StartIpcServerAsync(IIpcService ipcService)
    {
        try
        {
            await ipcService.StartServerAsync();
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 启动IPC服务器失败: {ex.Message}");
        }
    }

    #endregion
}