using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ComputerCompanion.Models;
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
    private static MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            // 初始化服务
            _settingsService = new SettingsService();
            _hardwareMonitorService = new HardwareMonitorService();
            _hardwareMonitorService.Start();

            var settings = _settingsService.GetSettings();

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

    /// <summary>
    /// 初始化悬浮窗模式
    /// </summary>
    private void InitializeOverlayMode(Settings settings)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var overlayWindow = new OverlayWindow();
            overlayWindow.Initialize(new OverlayViewModel(_hardwareMonitorService ?? throw new InvalidOperationException("硬件监控服务未初始化"), settings));
            desktop.MainWindow = overlayWindow;

            // 初始化 IPC 客户端
            _ipcService = new IpcService();
            _ipcService.MessageReceived += OnIpcMessageReceived;
            _ = ConnectIpcAsync();

            // 通知主程序悬浮窗已准备就绪
            _ = SendIpcMessageAsync(IpcMessageTypes.OverlayReady, "悬浮窗已启动");
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
                DataContext = new MainWindowViewModel(_hardwareMonitorService ?? throw new InvalidOperationException("硬件监控服务未初始化"), settings)
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
            _ = StartIpcServerAsync();

            // 初始化系统托盘
            _trayIconService = new TrayIconService();
            _trayIconService.ShowMainWindow += (s, e) => ShowMainWindow();
            _trayIconService.ExitApplication += (s, e) => ExitApplication();
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
        if (IsOverlayMode && _settingsService != null)
        {
            _settingsService.LoadSettings();
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
        // 悬浮窗已准备好，可以发送初始设置等
        Console.WriteLine("悬浮窗已成功启动并准备就绪");
        // 这里可以添加更多初始化逻辑，比如向悬浮窗发送当前设置
        if (!IsOverlayMode && _settingsService != null)
        {
            var settings = _settingsService.GetSettings();
            Console.WriteLine($"悬浮窗初始化配置: 显示FPS={settings.OverlayShowFPS}, 显示GPU={settings.OverlayShowGpu}");
        }
    }

    /// <summary>
    /// 发送 IPC 消息
    /// </summary>
    public static async Task SendIpcMessageAsync(string type, string data = "")
    {
        if (_ipcService != null)
        {
            try
            {
                await _ipcService.SendMessageAsync(new IpcMessage { Type = type, Data = data });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送IPC消息失败: {ex.Message}");
            }
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
        _ipcService?.Dispose();
        _hardwareMonitorService?.Dispose();
        
        Environment.Exit(0);
    }

    #endregion

    #region 辅助方法

    private async Task ConnectIpcAsync()
    {
        try
        {
            if (_ipcService != null)
            {
                await _ipcService.ConnectAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"IPC连接失败: {ex.Message}");
        }
    }

    private async Task StartIpcServerAsync()
    {
        try
        {
            if (_ipcService != null)
            {
                await _ipcService.StartServerAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动IPC服务器失败: {ex.Message}");
        }
    }

    #endregion
}