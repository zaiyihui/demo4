using Avalonia.Controls;
using ComputerCompanion.Views;
using System;

namespace ComputerCompanion.Services;

/// <summary>
/// 窗口管理器实现
/// 从 App.axaml.cs 中抽取的窗口管理逻辑
/// </summary>
public class WindowManager : IWindowManager, IDisposable
{
    #region 私有字段
    
    private MainWindow? _mainWindow;
    private bool _closeToHideConfigured;
    private bool _isDisposed;
    
    #endregion
    
    #region 公共属性和事件
    
    public MainWindow? MainWindow => _mainWindow;
    
    public bool IsMainWindowVisible
    {
        get
        {
            try
            {
                return _mainWindow != null && _mainWindow.IsVisible;
            }
            catch
            {
                return false;
            }
        }
    }
    
    public event EventHandler? ExitRequested;
    
    #endregion
    
    #region 公共方法
    
    public void SetMainWindow(MainWindow window)
    {
        if (_mainWindow != null)
        {
            Program.Log("[窗口管理] 主窗口已存在，将被替换");
        }
        
        _mainWindow = window;
        Program.Log("[窗口管理] 主窗口已设置");
    }
    
    public void ShowMainWindow()
    {
        try
        {
            if (_mainWindow == null)
            {
                Program.Log("[窗口管理] ShowMainWindow: 主窗口为空");
                return;
            }
            
            Program.Log("[窗口管理] 显示主窗口");
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.WindowState = WindowState.Normal;
        }
        catch (Exception ex)
        {
            Program.Log($"[窗口管理] ShowMainWindow 失败: {ex.Message}");
        }
    }
    
    public void HideMainWindow()
    {
        try
        {
            if (_mainWindow == null)
            {
                Program.Log("[窗口管理] HideMainWindow: 主窗口为空");
                return;
            }
            
            Program.Log("[窗口管理] 隐藏主窗口");
            _mainWindow.Hide();
        }
        catch (Exception ex)
        {
            Program.Log($"[窗口管理] HideMainWindow 失败: {ex.Message}");
        }
    }
    
    public void ToggleMainWindow()
    {
        if (IsMainWindowVisible)
        {
            HideMainWindow();
        }
        else
        {
            ShowMainWindow();
        }
    }
    
    public void ConfigureCloseToHideBehavior()
    {
        if (_mainWindow == null || _closeToHideConfigured)
            return;
        
        _mainWindow.Closing += OnMainWindowClosing;
        _closeToHideConfigured = true;
        Program.Log("[窗口管理] 已配置关闭按钮行为：隐藏而非退出");
    }
    
    #endregion
    
    #region 私有方法
    
    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        HideMainWindow();
        Program.Log("[窗口管理] 主窗口关闭按钮 -> 隐藏到托盘");
    }
    
    #endregion
    
    #region IDisposable 实现
    
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        
        try
        {
            if (_mainWindow != null && _closeToHideConfigured)
            {
                _mainWindow.Closing -= OnMainWindowClosing;
                Program.Log("[窗口管理] 已取消关闭事件订阅");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[窗口管理] 释放资源异常: {ex.Message}");
        }
        
        _mainWindow = null;
        Program.Log("[窗口管理] 已释放");
    }
    
    #endregion
}