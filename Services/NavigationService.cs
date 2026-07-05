using Avalonia.Controls;
using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Models;
using System;

namespace ComputerCompanion.Services;

public class NavigationService : INavigationService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowManager _windowManager;
    private Window? _mainWindow;
    private bool _isDisposed;

    public NavigationService(IServiceProvider serviceProvider, IWindowManager windowManager)
    {
        _serviceProvider = serviceProvider;
        _windowManager = windowManager;
        Program.Log("[导航] NavigationService 已初始化");
    }

    public Window? MainWindow => _mainWindow;

    public void SetMainWindow(Window window)
    {
        try
        {
            if (_mainWindow != null && _mainWindow != window)
            {
                Program.Log("[导航] SetMainWindow: 替换已存在的主窗口");
            }
            
            _mainWindow = window;
            _windowManager.SetMainWindow(window);
            Program.Log("[导航] SetMainWindow: 主窗口已设置");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] SetMainWindow 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void ShowSettings()
    {
        try
        {
            Program.Log("[导航] ShowSettings: 开始打开设置窗口");
            
            var settingsService = _serviceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService == null)
            {
                Program.Log("[导航] ShowSettings: ISettingsService 服务获取失败");
                return;
            }

            var settings = settingsService.GetSettings();
            if (settings == null)
            {
                Program.Log("[导航] ShowSettings: 设置对象为空，使用默认设置");
                settings = new Settings();
            }

            var settingsWindow = new Views.SettingsWindow(settings, OnSettingsSaved);
            
            if (_mainWindow != null)
            {
                settingsWindow.ShowDialog(_mainWindow);
            }
            else
            {
                settingsWindow.Show();
                Program.Log("[导航] ShowSettings: 主窗口为空，以独立窗口方式显示设置");
            }
            
            Program.Log("[导航] ShowSettings: 设置窗口已成功打开");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] ShowSettings 失败: {ex.GetType().Name}: {ex.Message}");
            Program.Log(ex.StackTrace ?? "[导航] 无堆栈信息");
        }
    }

    public void ShowPerformanceDashboard()
    {
        try
        {
            Program.Log("[导航] ShowPerformanceDashboard: 性能监控面板已是主窗口，直接激活");
            _mainWindow?.Activate();
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] ShowPerformanceDashboard 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void ShowMainWindow()
    {
        try
        {
            Program.Log("[导航] ShowMainWindow: 开始显示主窗口");
            _windowManager.ShowMainWindow();
            Program.Log("[导航] ShowMainWindow: 主窗口显示完成");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] ShowMainWindow 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void HideMainWindow()
    {
        try
        {
            Program.Log("[导航] HideMainWindow: 开始隐藏主窗口");
            _windowManager.HideMainWindow();
            Program.Log("[导航] HideMainWindow: 主窗口隐藏完成");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] HideMainWindow 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void ToggleMainWindow()
    {
        try
        {
            Program.Log("[导航] ToggleMainWindow: 切换主窗口可见性");
            _windowManager.ToggleMainWindow();
            Program.Log("[导航] ToggleMainWindow: 切换完成");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] ToggleMainWindow 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void ClosePerformanceDashboard()
    {
        try
        {
            Program.Log("[导航] ClosePerformanceDashboard: 性能监控面板已是主窗口，调用CloseAll");
            CloseAll();
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] ClosePerformanceDashboard 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void SwitchOverlayViewMode()
    {
        try
        {
            Program.Log("[导航] SwitchOverlayViewMode: 切换悬浮窗视图模式");
            
            var overlayManager = _serviceProvider.GetService(typeof(IOverlayProcessManager)) as IOverlayProcessManager;
            if (overlayManager != null)
            {
                var ipcService = _serviceProvider.GetService(typeof(IIpcService)) as IIpcService;
                if (ipcService != null && ipcService.IsConnected)
                {
                    _ = App.SendIpcMessageAsync(ipcService, IpcMessageTypes.SwitchViewMode);
                    Program.Log("[导航] SwitchOverlayViewMode: 已发送切换视图模式消息");
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] SwitchOverlayViewMode 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void CloseAll()
    {
        try
        {
            Program.Log("[导航] CloseAll: 开始关闭所有窗口");
            
            if (_mainWindow != null)
            {
                _mainWindow.Close();
                Program.Log("[导航] CloseAll: 主窗口已关闭");
            }
            
            Program.Log("[导航] CloseAll: 所有窗口关闭完成");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] CloseAll 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnSettingsSaved(Settings settings)
    {
        try
        {
            Program.Log("[导航] OnSettingsSaved: 设置已保存");

            var settingsService = _serviceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null)
            {
                settingsService.SaveSettings();
                Program.Log("[导航] OnSettingsSaved: 设置已持久化");
            }

            var ipcService = _serviceProvider.GetService(typeof(IIpcService)) as IIpcService;
            if (ipcService != null && ipcService.IsConnected)
            {
                _ = App.SendIpcMessageAsync(ipcService, IpcMessageTypes.SettingsChanged);
                Program.Log("[导航] OnSettingsSaved: 已发送设置变更消息");
            }

            var overlayManager = _serviceProvider.GetService(typeof(IOverlayProcessManager)) as IOverlayProcessManager;
            if (overlayManager != null)
            {
                if (settings.Overlay.EnableOverlay && !overlayManager.IsRunning)
                {
                    overlayManager.Start();
                    Program.Log("[导航] OnSettingsSaved: 已启动悬浮窗");
                }
                else if (!settings.Overlay.EnableOverlay && overlayManager.IsRunning)
                {
                    overlayManager.Stop();
                    Program.Log("[导航] OnSettingsSaved: 已停止悬浮窗");
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] OnSettingsSaved 处理失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        try
        {
            Program.Log("[导航] Dispose: 开始释放资源");
            
            _mainWindow = null;
            
            Program.Log("[导航] Dispose: 资源释放完成");
        }
        catch (Exception ex)
        {
            Program.Log($"[导航] Dispose 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }
}