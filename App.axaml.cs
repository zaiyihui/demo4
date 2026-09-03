using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ComputerCompanion.Models;
using ComputerCompanion.ViewModels;
using ComputerCompanion.Views;
using ComputerCompanion.Services;
using ComputerCompanion.Core.Abstractions;
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
        services.AddSingleton<FpsMonitorService>();
        services.AddSingleton<IHardwareMonitorService>(sp =>
        {
            var svc = new HardwareMonitorService();
            // 注入 ETW FPS 监控（通过反射设置私有字段）
            var fpsMonitor = sp.GetRequiredService<FpsMonitorService>();
            var field = typeof(HardwareMonitorService).GetField("_fpsMonitor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(svc, fpsMonitor);
            return svc;
        });
        // GPU 风扇曲线控制服务（在 IHardwareMonitorService 之后注册；
        // Initialize 在 HardwareMonitorService.Start() 完成后再调用，因为 Computer 对象在 Start() 中创建）
        services.AddSingleton<FanControlService>();
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

        // 新增核心服务 - 性能监控、日志、备份等
        services.AddSingleton<Core.Abstractions.ILogService, Core.Services.LogService>();
        services.AddSingleton<Core.Abstractions.IPerformanceMonitorService, Core.Services.PerformanceMonitorService>();
        services.AddSingleton<Core.Abstractions.IBackupService>(sp =>
            new Core.Services.BackupService(
                sp.GetRequiredService<ISettingsService>()));
        services.AddSingleton<Core.Abstractions.ILocalizationService, Core.Services.LocalizationService>();
        services.AddSingleton<Core.Abstractions.IInsightService>(sp =>
            new Core.Services.InsightService(
                sp.GetRequiredService<Core.Abstractions.IPerformanceMonitorService>(),
                sp.GetRequiredService<IHardwareMonitorService>()));
        services.AddSingleton<Core.Abstractions.IPluginService, Core.Services.PluginService>();

            // 图表服务
            services.AddSingleton<IChartService, ChartService>();
            
            // 告警声音服务
            services.AddSingleton<IAlertSoundService, AlertSoundService>();
            
            // 主题服务
            services.AddSingleton<IThemeService, ThemeService>();
            
            // 告警规则服务
            services.AddSingleton<IAlertRuleService, AlertRuleService>();

            // 导航服务
            services.AddSingleton<INavigationService, NavigationService>();

            // 全局热键服务
            services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();

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
            
            Program.Log("[应用] 更新日志路径前");
            Program.UpdateLogPath(dataStorageService.GetLogPath());
            Program.Log("[应用] 更新日志路径后");

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

                    // 硬件监控启动后，初始化 GPU 风扇控制服务（Computer 对象在 Start() 中创建）
                    try
                    {
                        var fanControlService = ServiceProvider.GetRequiredService<FanControlService>();
                        var computerField = typeof(HardwareMonitorService).GetField("_computer",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var computer = computerField?.GetValue(hardwareMonitorService) as LibreHardwareMonitor.Hardware.Computer;
                        if (computer != null)
                        {
                            fanControlService.Initialize(computer);
                            Program.Log($"[应用] 风扇控制服务初始化完成，可用={fanControlService.IsFanControlAvailable}");
                        }
                        else
                        {
                            Program.Log("[应用] 风扇控制服务初始化跳过：Computer 对象为空");
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Log($"[应用] 风扇控制服务初始化失败: {ex.Message}");
                    }
                    
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

    /// <summary>
    /// 打开设置窗口
    /// </summary>
    private void OpenSettingsWindow()
    {
        try
        {
            Program.Log("[应用] 打开设置窗口（通过导航服务）");
            
            var navigationService = ServiceProvider.GetService<INavigationService>();
            if (navigationService != null)
            {
                navigationService.ShowSettings();
            }
            else
            {
                Program.Log("[应用] OpenSettingsWindow: INavigationService 获取失败，回退到直接创建");
                
                var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
                var settings = settingsService.GetSettings();
                
                var settingsWindow = new Views.SettingsWindow(settings, (updatedSettings) => {
                    settingsService.SaveSettings();
                });
                
                settingsWindow.Show();
                settingsWindow.Activate();
            }
            
            Program.Log("[应用] 设置窗口已打开");
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 打开设置窗口失败: {ex.Message}");
        }
    }

    private void InitializeMainMode(Settings settings)
    {
        Program.Log("[应用] InitializeMainMode 开始");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                Program.Log("[应用] 获取 desktop lifetime");
                _desktopLifetime = desktop;
                
                Program.Log("[应用] 获取性能监控服务");
                var performanceMonitor = ServiceProvider.GetRequiredService<IPerformanceMonitorService>();
                
                Program.Log("[应用] 获取硬件监控服务");
                var hardwareMonitor = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
                
                Program.Log("[应用] 获取日志服务");
                var logService = ServiceProvider.GetRequiredService<ILogService>();
                
                Program.Log("[应用] 获取告警声音服务");
                var alertSoundService = ServiceProvider.GetRequiredService<IAlertSoundService>();

                Program.Log("[应用] 创建性能监控面板视图模型");
                var viewModel = new PerformanceDashboardViewModel(performanceMonitor, hardwareMonitor, logService, alertSoundService);
                
                Program.Log("[应用] 创建性能监控面板窗口");
                var dashboardWindow = new PerformanceDashboardWindow();
                Program.Log("[应用] 窗口已创建");
                dashboardWindow.SetViewModel(viewModel);
                Program.Log("[应用] ViewModel已设置");

                var windowManager = ServiceProvider.GetRequiredService<IWindowManager>();
                windowManager.SetMainWindow(dashboardWindow);
                Program.Log("[应用] 窗口已设置到窗口管理器");

                var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
                navigationService.SetMainWindow(dashboardWindow);

                desktop.MainWindow = dashboardWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Program.Log("[应用] 窗口已设置为MainWindow");

                dashboardWindow.Show();
                Program.Log("[应用] 窗口已调用Show()");
                dashboardWindow.Activate();
                Program.Log("[应用] 窗口已调用Activate()");
                Program.Log("[应用] 性能监控面板已显示");

                // 管理员权限引导（仅主进程、非管理员时提示一次）
                if (!Program.IsRunningAsAdmin && settings.Startup != null && settings.Startup.HasShownAdminPrompt)
                {
                    _ = ShowAdminPromptAsync(dashboardWindow);
                    settings.Startup.HasShownAdminPrompt = true;
                    var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
                    settingsService.SaveSettings();
                }
                else if (!Program.IsRunningAsAdmin)
                {
                    _ = ShowAdminPromptAsync(dashboardWindow);
                }

                var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
                var router = ServiceProvider.GetRequiredService<IIpcMessageRouter>();
                
                RegisterMainModeMessageHandlers(router);
                
                router.Start();
                _ = StartIpcServerAsync(ipcService);

                try
                {
                    var trayIconService = ServiceProvider.GetRequiredService<TrayIconService>();
                    trayIconService.ShowMainWindow += (s, e) => windowManager.ShowMainWindow();
                    trayIconService.OpenSettings += (s, e) => navigationService.ShowSettings();
                    trayIconService.ExitApplication += (s, e) => ExitApplication();
                    trayIconService.Initialize();
                    Program.Log("[应用] 托盘图标服务已初始化");
                }
                catch (Exception ex)
                {
                    Program.Log($"[应用] 托盘初始化失败（忽略）: {ex.Message}");
                }

                var overlayManager = ServiceProvider.GetRequiredService<IOverlayProcessManager>();
                overlayManager.ProcessExited += OnOverlayProcessExited;
                
                if (settings.Overlay.EnableOverlay)
                {
                    Program.Log("[应用] 配置启用悬浮窗，启动悬浮窗进程");
                    overlayManager.Start();
                }

                var hotkeyService = ServiceProvider.GetRequiredService<IGlobalHotkeyService>() as GlobalHotkeyService;
                if (hotkeyService != null)
                {
                    try
                    {
                        var platformImpl = dashboardWindow.PlatformImpl;
                        var handle = platformImpl != null ? GetWindowHandle(platformImpl) : IntPtr.Zero;
                        if (handle != IntPtr.Zero)
                        {
                            hotkeyService.Initialize(handle);
                            
                            hotkeyService.ToggleOverlay += () => 
                            {
                                if (overlayManager.IsRunning)
                                {
                                    overlayManager.Stop();
                                }
                                else
                                {
                                    overlayManager.Start();
                                }
                            };
                            hotkeyService.SwitchViewMode += () => 
                            {
                                var navigationService = ServiceProvider.GetService<INavigationService>();
                                navigationService?.SwitchOverlayViewMode();
                            };
                            hotkeyService.RegisterHotkeys();
                        }
                        else
                        {
                            Program.Log("[热键] 无法获取窗口句柄，热键功能不可用");
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Log($"[热键] 初始化失败: {ex.Message}");
                    }
                }

                Program.Log("[应用] 性能监控面板模式初始化完成");
            }
            catch (Exception ex)
            {
                Program.Log($"[应用] 主窗口初始化失败: {ex.GetType().Name}: {ex.Message}");
                Program.Log(ex.StackTrace ?? "无堆栈信息");
                try
                {
                    var dashboardWindow = new PerformanceDashboardWindow();
                    desktop.MainWindow = dashboardWindow;
                    dashboardWindow.Show();
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
        
        router.RegisterHandler(IpcMessageTypes.ShowSettings, msg =>
        {
            var navigationService = ServiceProvider.GetService<INavigationService>();
            navigationService?.ShowSettings();
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

    public static Window? MainWindow => 
        _serviceProvider?.GetService<IWindowManager>()?.MainWindow;

    public static void ShowMainWindow()
    {
        var windowManager = _serviceProvider?.GetService<IWindowManager>();
        windowManager?.ShowMainWindow();
    }

    /// <summary>
    /// 管理员权限引导对话框
    /// </summary>
    private async Task ShowAdminPromptAsync(Avalonia.Controls.Window owner)
    {
        try
        {
            await Task.Delay(1500); // 等主窗口渲染完

            var dialog = new Avalonia.Controls.Window
            {
                Title = "权限提示",
                Width = 420,
                Height = 220,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1a1a2e"))
            };

            var panel = new Avalonia.Controls.StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 12
            };

            var title = new Avalonia.Controls.TextBlock
            {
                Text = "管理员权限建议",
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.White
            };
            panel.Children.Add(title);

            var msg = new Avalonia.Controls.TextBlock
            {
                Text = "当前未以管理员身份运行，以下功能受限：\n\n• CPU/GPU 温度传感器数据不完整\n• ETW 真实游戏 FPS 监控不可用\n• 风扇转速读取受限\n\n是否以管理员身份重启以获取完整功能？",
                FontSize = 12,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#ccc")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
            panel.Children.Add(msg);

            var btnPanel = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8
            };

            var btnRestart = new Avalonia.Controls.Button
            {
                Content = "以管理员重启",
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#00db78")),
                Foreground = Avalonia.Media.Brushes.Black
            };
            btnRestart.Click += (s, e) =>
            {
                Program.RestartAsAdmin();
                dialog.Close();
                ExitApplication();
            };
            btnPanel.Children.Add(btnRestart);

            var btnSkip = new Avalonia.Controls.Button
            {
                Content = "以后再说",
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#333")),
                Foreground = Avalonia.Media.Brushes.White
            };
            btnSkip.Click += (s, e) => dialog.Close();
            btnPanel.Children.Add(btnSkip);

            panel.Children.Add(btnPanel);
            dialog.Content = panel;

            await dialog.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Program.Log($"[权限] 管理员引导对话框异常: {ex.Message}");
        }
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
            var fanControlService = _serviceProvider?.GetService<FanControlService>();

            trayIconService?.Dispose();
            ipcService?.Dispose();
            fanControlService?.Dispose();
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

    private IntPtr GetWindowHandle(object platformImpl)
    {
        try
        {
            var handleProperty = platformImpl.GetType().GetProperty("Handle");
            if (handleProperty != null)
            {
                return (IntPtr)handleProperty.GetValue(platformImpl)!;
            }
            
            var handleField = platformImpl.GetType().GetField("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (handleField != null)
            {
                return (IntPtr)handleField.GetValue(platformImpl)!;
            }
        }
        catch { }
        
        return IntPtr.Zero;
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
