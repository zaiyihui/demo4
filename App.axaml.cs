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

        // 核心服务
        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDataStorageService, DataStorageService>();
        
        // 拆分的监控服务
        services.AddSingleton<INetworkMonitorService, NetworkMonitorService>();
        services.AddSingleton<ILatencyMonitorService, LatencyMonitorService>();
        services.AddSingleton<IBatteryMonitorService, BatteryMonitorService>();
        
        // IPC 服务
        services.AddSingleton<IIpcService>(sp =>
            new IpcService(sp.GetService<ISecurityService>()));
        services.AddSingleton<IIpcMessageRouter, IpcMessageRouter>();
        
        // 新抽取的管理服务
        services.AddSingleton<IOverlayProcessManager>(sp =>
            new OverlayProcessManager(sp.GetService<IIpcService>()));
        services.AddSingleton<IWindowManager, WindowManager>();
        
        // 托盘服务
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
                    
                    Program.Log("[应用] 后台启动网络监控");
                    var networkMonitorService = ServiceProvider.GetRequiredService<INetworkMonitorService>();
                    networkMonitorService.Start();
                    
                    Program.Log("[应用] 后台启动延迟监控");
                    var latencyMonitorService = ServiceProvider.GetRequiredService<ILatencyMonitorService>();
                    latencyMonitorService.Start();
                    
                    Program.Log("[应用] 后台启动电池监控");
                    var batteryMonitorService = ServiceProvider.GetRequiredService<IBatteryMonitorService>();
                    batteryMonitorService.Start();
                }
                catch (Exception ex)
                {
                    Program.Log($"[应用] 监控服务后台启动失败: {ex.Message}");
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
            var latencyMonitor = ServiceProvider.GetRequiredService<ILatencyMonitorService>();
            var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
            var overlayWindow = new OverlayWindow();
            overlayWindow.Initialize(new OverlayViewModel(monitor, latencyMonitor, settings));
            desktop.MainWindow = overlayWindow;

            var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
            var router = ServiceProvider.GetRequiredService<IIpcMessageRouter>();
            
            // 注册悬浮窗模式的 IPC 消息处理器
            RegisterOverlayMessageHandlers(router, settingsService);
            
            router.Start();
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
                
                Program.Log("[应用] 获取网络监控服务");
                var networkMonitor = ServiceProvider.GetRequiredService<INetworkMonitorService>();
                
                Program.Log("[应用] 获取延迟监控服务");
                var latencyMonitor = ServiceProvider.GetRequiredService<ILatencyMonitorService>();
                
                Program.Log("[应用] 获取电池监控服务");
                var batteryMonitor = ServiceProvider.GetRequiredService<IBatteryMonitorService>();
                
                Program.Log("[应用] 获取设置服务");
                var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
                if (settingsService == null)
                {
                    Program.Log("[应用] 设置服务为 null");
                    throw new InvalidOperationException("设置服务为 null");
                }

                Program.Log("[应用] 创建主窗口视图模型");
                var viewModel = new MainWindowViewModel(monitor, networkMonitor, latencyMonitor, batteryMonitor, settings, settingsService);
                
                Program.Log("[应用] 创建主窗口");
                var mainWindow = new MainWindow
                {
                    DataContext = viewModel
                };

                // 使用窗口管理器
                var windowManager = ServiceProvider.GetRequiredService<IWindowManager>();
                windowManager.SetMainWindow(mainWindow);
                windowManager.ConfigureCloseToHideBehavior();

                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // 先显示窗口
                mainWindow.Show();
                mainWindow.Activate();
                Program.Log("[应用] 主窗口已显示");

                // IPC 服务和路由器
                var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
                var router = ServiceProvider.GetRequiredService<IIpcMessageRouter>();
                
                // 注册主窗口模式的 IPC 消息处理器
                RegisterMainModeMessageHandlers(router);
                
                router.Start();
                _ = StartIpcServerAsync(ipcService);

                // 托盘图标
                try
                {
                    var trayIconService = ServiceProvider.GetRequiredService<TrayIconService>();
                    trayIconService.ShowMainWindow += (s, e) => windowManager.ShowMainWindow();
                    trayIconService.ExitApplication += (s, e) => ExitApplication();
                    trayIconService.Initialize();
                    Program.Log("[应用] 托盘图标服务已初始化");
                }
                catch (Exception ex)
                {
                    Program.Log($"[应用] 托盘初始化失败（忽略）: {ex.Message}");
                }

                // 悬浮窗进程管理
                var overlayManager = ServiceProvider.GetRequiredService<IOverlayProcessManager>();
                overlayManager.ProcessExited += OnOverlayProcessExited;
                
                if (settings.Overlay.EnableOverlay)
                {
                    Program.Log("[应用] 配置启用悬浮窗，启动悬浮窗进程");
                    overlayManager.Start();
                }

                // 最小化启动
                if (settings.Startup.StartMinimized)
                {
                    Program.Log("[应用] 设置为最小化启动 -> 隐藏主窗口");
                    windowManager.HideMainWindow();
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
                    var mainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(
                            new HardwareMonitorService(),
                            new NetworkMonitorService(),
                            new LatencyMonitorService(),
                            new BatteryMonitorService(),
                            settings,
                            new SettingsService())
                    };
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
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

    #region IPC 消息处理器注册

    private void RegisterOverlayMessageHandlers(IIpcMessageRouter router, ISettingsService settingsService)
    {
        router.RegisterHandler(IpcMessageTypes.SettingsChanged, msg =>
        {
            settingsService?.LoadSettings();
        });
        
        router.RegisterHandler(IpcMessageTypes.ExitApplication, msg =>
        {
            Environment.Exit(0);
        });
        
        router.RegisterHandler(IpcMessageTypes.Heartbeat, msg =>
        {
            Program.Log("[应用] 收到悬浮窗心跳");
        });
        
        router.RegisterHandler(IpcMessageTypes.Error, msg =>
        {
            Program.Log($"[应用] 收到错误消息: {msg.Data}");
        });
    }

    private void RegisterMainModeMessageHandlers(IIpcMessageRouter router)
    {
        var windowManager = ServiceProvider.GetService<IWindowManager>();
        var overlayManager = ServiceProvider.GetService<IOverlayProcessManager>();
        var settingsService = ServiceProvider.GetService<ISettingsService>();
        
        router.RegisterHandler(IpcMessageTypes.SettingsChanged, msg =>
        {
            settingsService?.LoadSettings();
        });
        
        router.RegisterHandler(IpcMessageTypes.ShowMainWindow, msg =>
        {
            windowManager?.ShowMainWindow();
        });
        
        router.RegisterHandler(IpcMessageTypes.ExitApplication, msg =>
        {
            ExitApplication();
        });
        
        router.RegisterHandler(IpcMessageTypes.OverlayReady, msg =>
        {
            Program.Log("[应用] 悬浮窗已成功启动并准备就绪");
            if (settingsService != null)
            {
                var settings = settingsService.GetSettings();
                Program.Log($"[应用] 悬浮窗配置: FPS={settings.Overlay.OverlayShowFPS}, GPU={settings.Overlay.OverlayShowGpu}");
            }
        });
        
        router.RegisterHandler(IpcMessageTypes.Heartbeat, msg =>
        {
            Program.Log("[应用] 收到悬浮窗心跳");
        });
        
        router.RegisterHandler(IpcMessageTypes.StatusUpdate, msg =>
        {
            Program.Log($"[应用] 收到状态更新: {msg.Data}");
        });
        
        router.RegisterHandler(IpcMessageTypes.ToggleOverlay, msg =>
        {
            if (overlayManager == null) return;
            
            if (overlayManager.IsRunning)
            {
                overlayManager.Stop();
            }
            else
            {
                overlayManager.Start();
            }
        });
        
        router.RegisterHandler(IpcMessageTypes.Error, msg =>
        {
            Program.Log($"[应用] 收到错误消息: {msg.Data}");
        });
    }

    #endregion

    #region 悬浮窗进程事件处理

    private void OnOverlayProcessExited(object? sender, OverlayProcessExitedEventArgs e)
    {
        Program.Log($"[应用] 悬浮窗进程退出事件: 退出码={e.ExitCode}, 异常退出={e.IsUnexpectedExit}");
        
        // 检查是否需要自动恢复
        if (e.IsUnexpectedExit)
        {
            var settingsService = _serviceProvider?.GetService<ISettingsService>();
            if (settingsService?.GetSettings()?.Overlay.EnableOverlay == true)
            {
                var overlayManager = _serviceProvider?.GetService<IOverlayProcessManager>();
                overlayManager?.TryAutoRecover();
            }
        }
    }

    #endregion

    #region 窗口管理（公共静态方法，保持向后兼容）

    public static MainWindow? MainWindow => 
        _serviceProvider?.GetService<IWindowManager>()?.MainWindow;

    public static void ShowMainWindow()
    {
        var windowManager = _serviceProvider?.GetService<IWindowManager>();
        windowManager?.ShowMainWindow();
    }

    public static void ExitApplication()
    {
        Program.Log("[应用] 正在退出程序");

        try
        {
            var overlayManager = _serviceProvider?.GetService<IOverlayProcessManager>();
            overlayManager?.Stop();
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 悬浮窗管理器停止异常: {ex.Message}");
        }

        try
        {
            var router = _serviceProvider?.GetService<IIpcMessageRouter>();
            router?.Stop();
        }
        catch { }

        try
        {
            var trayIconService = _serviceProvider?.GetService<TrayIconService>();
            var ipcService = _serviceProvider?.GetService<IIpcService>();
            var hardwareMonitorService = _serviceProvider?.GetService<IHardwareMonitorService>();
            var networkMonitorService = _serviceProvider?.GetService<INetworkMonitorService>();
            var latencyMonitorService = _serviceProvider?.GetService<ILatencyMonitorService>();
            var batteryMonitorService = _serviceProvider?.GetService<IBatteryMonitorService>();

            trayIconService?.Dispose();
            ipcService?.Dispose();
            hardwareMonitorService?.Dispose();
            networkMonitorService?.Dispose();
            latencyMonitorService?.Dispose();
            batteryMonitorService?.Dispose();
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
}