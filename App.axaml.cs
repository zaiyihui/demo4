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

    public static IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("服务容器未初始化");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
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
        try
        {
            var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
            var hardwareMonitorService = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
            hardwareMonitorService.Start();

            var settings = settingsService.GetSettings();

            if (IsOverlayMode)
            {
                InitializeOverlayMode(settings);
            }
            else
            {
                InitializeMainMode(settings);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"应用初始化失败: {ex.Message}");
        }

        base.OnFrameworkInitializationCompleted();
    }

    #region 初始化方法

    private void InitializeOverlayMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var monitor = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
            var overlayWindow = new OverlayWindow();
            overlayWindow.Initialize(new OverlayViewModel(monitor, settings));
            desktop.MainWindow = overlayWindow;

            var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
            ipcService.MessageReceived += OnIpcMessageReceived;
            _ = ConnectIpcAsync(ipcService);
            _ = SendIpcMessageAsync(ipcService, IpcMessageTypes.OverlayReady, "悬浮窗已启动");
        }
    }

    private void InitializeMainMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var monitor = ServiceProvider.GetRequiredService<IHardwareMonitorService>();
            var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
            
            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(monitor, settings, settingsService)
            };

            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                _mainWindow.Hide();
            };

            var ipcService = ServiceProvider.GetRequiredService<IIpcService>();
            ipcService.MessageReceived += OnIpcMessageReceived;
            _ = StartIpcServerAsync(ipcService);

            var trayIconService = ServiceProvider.GetRequiredService<TrayIconService>();
            trayIconService.ShowMainWindow += (s, e) => ShowMainWindow();
            trayIconService.ExitApplication += (s, e) => ExitApplication();
            trayIconService.Initialize();

            if (settings.EnableOverlay)
            {
                StartOverlayProcess();
            }

            if (settings.StartMinimized)
            {
                _mainWindow.Hide();
            }
        }
    }

    #endregion

    #region IPC 消息处理

    private void OnIpcMessageReceived(IpcMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Type))
        {
            return;
        }

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
        if (!IsOverlayMode)
        {
            ShowMainWindow();
        }
    }

    private void HandleExitApplication()
    {
        if (IsOverlayMode)
        {
            Environment.Exit(0);
        }
    }

    private void HandleOverlayReady()
    {
        Console.WriteLine("悬浮窗已成功启动并准备就绪");
        if (!IsOverlayMode)
        {
            var settingsService = ServiceProvider.GetService<ISettingsService>();
            if (settingsService != null)
            {
                var settings = settingsService.GetSettings();
                Console.WriteLine($"悬浮窗初始化配置: 显示FPS={settings.OverlayShowFPS}, 显示GPU={settings.OverlayShowGpu}");
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
            Console.WriteLine($"发送IPC消息失败: {ex.Message}");
        }
    }

    #endregion

    #region 悬浮窗进程管理

    public static void StartOverlayProcess()
    {
        try
        {
            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                return;
            }

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("无法获取应用程序路径");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = Program.OverlayModeArg,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = System.IO.Path.GetDirectoryName(exePath)
            };

            _overlayProcess = Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动悬浮窗进程失败: {ex.Message}");
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
            Console.WriteLine($"停止悬浮窗进程失败: {ex.Message}");
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
        _mainWindow?.Show();
        _mainWindow?.Activate();
    }

    public static void ExitApplication()
    {
        StopOverlayProcess();
        
        var trayIconService = _serviceProvider?.GetService<TrayIconService>();
        var ipcService = _serviceProvider?.GetService<IIpcService>();
        var hardwareMonitorService = _serviceProvider?.GetService<IHardwareMonitorService>();
        
        trayIconService?.Dispose();
        ipcService?.Dispose();
        hardwareMonitorService?.Dispose();
        
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
            Console.WriteLine($"IPC连接失败: {ex.Message}");
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
            Console.WriteLine($"启动IPC服务器失败: {ex.Message}");
        }
    }

    #endregion
}