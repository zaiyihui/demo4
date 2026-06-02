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
            var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
            var settings = settingsService.GetSettings();

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
            _desktopLifetime = desktop;
            var monitor = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
            var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();

            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(monitor, settings, settingsService)
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
        }
    }

    private void HandleSettingsChanged()
    {
        if (IsOverlayMode)
        {
            var settingsService = ServiceProvider.GetService<ISettingsService>();
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
            var settingsService = ServiceProvider.GetService<ISettingsService>();
            if (settingsService != null)
            {
                var settings = settingsService.GetSettings();
                Program.Log($"[应用] 悬浮窗配置: FPS={settings.Overlay.OverlayShowFPS}, GPU={settings.Overlay.OverlayShowGpu}");
            }
        }
    }

    private static async Task SendIpcMessageAsync(IIpcService ipcService, string type, string data = "")
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

    public static void StartOverlayProcess()
    {
        try
        {
            if (_overlayProcess != null && !_overlayProcess.HasExited) return;

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
            Program.Log($"[应用] 悬浮窗进程已启动 PID={_overlayProcess?.Id}");
        }
        catch (Exception ex)
        {
            Program.Log($"[应用] 启动悬浮窗进程失败: {ex.Message}");
        }
    }

    public static void StopOverlayProcess()
    {
        try
        {
            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                var ipcService = _serviceProvider?.GetService<IIpcService>();
                if (ipcService != null)
                {
                    _ = SendIpcMessageAsync(ipcService, IpcMessageTypes.ExitApplication);
                }

                if (!_overlayProcess.WaitForExit(2000))
                {
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
        }
    }

    public static void RestartOverlayProcess()
    {
        StopOverlayProcess();
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