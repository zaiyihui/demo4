using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ComputerCompanion.ViewModels;
using ComputerCompanion.Views;
using ComputerCompanion.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ComputerCompanion;

public partial class App : Application
{
    // 全局状态
    public static bool IsOverlayMode { get; set; } = false;
    private static HardwareMonitorService? _hardwareMonitorService;
    private static SettingsService? _settingsService;
    private static Process? _overlayProcess;
    private static IpcService? _ipcService;
    private static TrayIconService? _trayIconService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 初始化服务
        _settingsService = new SettingsService();
        _hardwareMonitorService = new HardwareMonitorService();
        _hardwareMonitorService.Start();

        var settings = _settingsService.GetSettings();

        if (IsOverlayMode)
        {
            // 悬浮窗模式
            InitializeOverlayMode(settings);
        }
        else
        {
            // 主程序模式
            InitializeMainMode(settings);
        }

        base.OnFrameworkInitializationCompleted();
    }

    #region 初始化方法

    /// <summary>
    /// 初始化悬浮窗模式
    /// </summary>
    private void InitializeOverlayMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            
            var overlayWindow = new OverlayWindow();
            overlayWindow.Initialize(new OverlayViewModel(_hardwareMonitorService, settings));
            desktop.MainWindow = overlayWindow;
            
            // 初始化 IPC 客户端
            _ipcService = new IpcService();
            _ipcService.MessageReceived += OnIpcMessageReceived;
            _ = _ipcService.ConnectAsync();
        }
    }

    /// <summary>
    /// 初始化主程序模式
    /// </summary>
    private void InitializeMainMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 创建主窗口
            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_hardwareMonitorService, settings)
            };

            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                _mainWindow.Hide();
            };

            // 初始化 IPC 服务器
            _ipcService = new IpcService();
            _ipcService.MessageReceived += OnIpcMessageReceived;
            _ = _ipcService.StartServerAsync();

            // 初始化系统托盘
            _trayIconService = new TrayIconService(_settingsService);
            _trayIconService.Initialize();

            // 如果启用了悬浮窗，启动悬浮窗进程
            if (settings.EnableOverlay)
            {
                StartOverlayProcess();
            }

            // 如果设置了启动时最小化，隐藏主窗口
            if (settings.StartMinimized)
            {
                _mainWindow.Hide();
            }
        }
    }

    #endregion

    #region IPC 消息处理

    /// <summary>
    /// 处理 IPC 消息
    /// </summary>
    private void OnIpcMessageReceived(IpcMessage message)
    {
        switch (message.Type)
        {
            case IpcMessageTypes.SettingsChanged:
                // 悬浮窗收到设置变更消息，重新加载设置
                if (IsOverlayMode && _hardwareMonitorService != null && _settingsService != null)
                {
                    _settingsService.LoadSettings();
                    // 这里可以更新悬浮窗显示
                }
                break;

            case IpcMessageTypes.ShowMainWindow:
                // 显示主窗口
                if (IsOverlayMode)
                {
                    // 悬浮窗不能显示主窗口，忽略
                }
                break;

            case IpcMessageTypes.ExitApplication:
                // 退出应用
                if (IsOverlayMode)
                {
                    Environment.Exit(0);
                }
                break;

            case IpcMessageTypes.OverlayReady:
                // 悬浮窗已准备好
                break;
        }
    }

    /// <summary>
    /// 发送 IPC 消息
    /// </summary>
    public static async Task SendIpcMessageAsync(string type, string data = "")
    {
        if (_ipcService != null)
        {
            await _ipcService.SendMessageAsync(new IpcMessage { Type = type, Data = data });
        }
    }

    #endregion

    #region 悬浮窗进程管理

    /// <summary>
    /// 启动悬浮窗进程
    /// </summary>
    public static void StartOverlayProcess()
    {
        try
        {
            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                Arguments = Program.OverlayModeArg,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            _overlayProcess = Process.Start(processStartInfo);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 停止悬浮窗进程
    /// </summary>
    public static void StopOverlayProcess()
    {
        try
        {
            if (_overlayProcess != null && !_overlayProcess.HasExited)
            {
                // 发送退出消息
                _ = SendIpcMessageAsync(IpcMessageTypes.ExitApplication);
                
                // 等待进程退出
                if (!_overlayProcess.WaitForExit(2000))
                {
                    _overlayProcess.Kill();
                }
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _overlayProcess?.Dispose();
            _overlayProcess = null;
        }
    }

    /// <summary>
    /// 重启悬浮窗进程
    /// </summary>
    public static void RestartOverlayProcess()
    {
        StopOverlayProcess();
        StartOverlayProcess();
    }

    #endregion

    #region 服务访问

    /// <summary>
    /// 获取硬件监控服务
    /// </summary>
    public static HardwareMonitorService? HardwareMonitor => _hardwareMonitorService;

    /// <summary>
    /// 获取设置服务
    /// </summary>
    public static SettingsService? SettingsService => _settingsService;

    #endregion

    #region 窗口管理

    private static MainWindow? _mainWindow;

    /// <summary>
    /// 获取主窗口实例
    /// </summary>
    public static MainWindow? MainWindow => _mainWindow;

    /// <summary>
    /// 显示主窗口
    /// </summary>
    public static void ShowMainWindow()
    {
        _mainWindow?.Show();
        _mainWindow?.Activate();
    }

    /// <summary>
    /// 完全退出应用
    /// </summary>
    public static void ExitApplication()
    {
        StopOverlayProcess();
        _trayIconService?.Dispose();
        Environment.Exit(0);
    }

    #endregion
}
