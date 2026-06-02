using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ComputerCompanion.Models;
using ComputerCompanion.Services;
using ComputerCompanion.ViewModels;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Views;

/// <summary>
/// 主窗口类 - 悬浮窗形式的硬件监控界面
/// 支持鼠标穿透、窗口拖拽、快捷键操作
/// </summary>
public partial class MainWindow : Window
{
    #region Win32 API 声明

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    #endregion

    #region 私有字段

    /// <summary>
    /// 鼠标拖拽起始点
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// 是否启用鼠标穿透模式
    /// </summary>
    private bool _isClickThroughEnabled = true;

    #endregion

    #region 构造函数

    public MainWindow()
    {
        InitializeComponent();
        
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        Topmost = true;
        ShowInTaskbar = true;

        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        
        Opened += OnWindowOpened;
        KeyDown += OnKeyDown;
        Deactivated += OnDeactivated;
    }

    #endregion

    #region 事件处理

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        SetClickThrough(_isClickThroughEnabled);
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // 窗口失去焦点时自动启用穿透
        if (_isClickThroughEnabled)
        {
            SetClickThrough(true);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _startPoint = e.GetPosition(this);
            SetClickThrough(false);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _startPoint;
            Position = new PixelPoint(Position.X + (int)offset.X, Position.Y + (int)offset.Y);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetClickThrough(_isClickThroughEnabled);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                break;
            case Key.F1:
                _isClickThroughEnabled = !_isClickThroughEnabled;
                SetClickThrough(_isClickThroughEnabled);
                UpdateStatusText();
                break;
            case Key.F2:
                ToggleGameMode();
                break;
            case Key.F3:
                OpenSettings();
                break;
        }
    }

    #endregion

    #region 设置窗口

    private void OpenSettings()
    {
        SetClickThrough(false);
        
        var settingsService = App.ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService ?? new SettingsService();
        var settings = settingsService.GetSettings();
        
        var settingsWindow = new SettingsWindow(settings, OnSettingsSaved);
        settingsWindow.ShowDialog(this);
    }

    private void OnSettingsSaved(Settings settings)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.UpdateSettings(settings);
        }

        if (settings.Overlay.EnableOverlay)
        {
            App.RestartOverlayProcess();
        }
        else
        {
            App.StopOverlayProcess();
        }

        HandleAutoStart(settings.Startup.AutoStart);

        SetClickThrough(_isClickThroughEnabled);
    }
    
    private void HandleAutoStart(bool enable)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("自动启动功能仅支持 Windows 系统");
            return;
        }

        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            
            if (!ValidateExePath(exePath, out var validationError))
            {
                Console.WriteLine($"无效的应用程序路径，拒绝修改注册表: {validationError}");
                return;
            }

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (key == null)
            {
                Console.WriteLine("无法访问注册表键，可能权限不足");
                return;
            }

            const string appName = "电脑伴侣";

            if (enable)
            {
                ValidateRegistryValue(appName, exePath!);
                key.SetValue(appName, exePath);
                Console.WriteLine("已设置开机自动启动");
            }
            else
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName);
                    Console.WriteLine("已取消开机自动启动");
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"权限不足，无法修改注册表: {ex.Message}");
            Console.WriteLine("请以管理员身份运行此程序以启用自动启动功能");
        }
        catch (System.Security.SecurityException ex)
        {
            Console.WriteLine($"安全策略阻止了注册表访问: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"修改注册表失败: {ex.Message}");
        }
    }

    private bool ValidateExePath(string? exePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(exePath))
        {
            errorMessage = "路径为空";
            return false;
        }

        if (exePath.Length > 260)
        {
            errorMessage = "路径长度超过 Windows 限制(260字符)";
            return false;
        }

        if (exePath.Contains(".."))
        {
            errorMessage = "检测到路径遍历攻击尝试(..)";
            return false;
        }

        var dangerousChars = new[] { '|', '&', ';', '<', '>', '`', '$', '(', ')', '!', '@', '#', '%', '^', '*', '+' };
        if (dangerousChars.Any(c => exePath.Contains(c)))
        {
            errorMessage = $"检测到可疑的命令注入字符: {string.Join(", ", dangerousChars.Where(c => exePath.Contains(c)))}";
            return false;
        }

        try
        {
            var fullPath = System.IO.Path.GetFullPath(exePath);
            
            if (!fullPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "应用程序路径必须是 .exe 文件";
                return false;
            }
            
            if (!System.IO.File.Exists(fullPath))
            {
                errorMessage = "指定的应用程序文件不存在";
                return false;
            }

            if (!IsPathInSecureLocation(fullPath))
            {
                errorMessage = "应用程序路径不在安全目录中(仅允许 Program Files、AppData、用户目录)";
                return false;
            }

            if (!VerifyFileIntegrity(fullPath))
            {
                errorMessage = "文件完整性验证失败";
                Console.WriteLine("警告: 建议验证文件数字签名");
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"路径验证失败: {ex.Message}";
            return false;
        }

        return true;
    }

    private bool IsPathInSecureLocation(string fullPath)
    {
        var securePaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        return securePaths.Any(securePath => 
            fullPath.StartsWith(securePath, StringComparison.OrdinalIgnoreCase));
    }

    private bool VerifyFileIntegrity(string filePath)
    {
        try
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return false;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateRegistryValue(string valueName, string valueData)
    {
        if (valueName.Contains("\\") || valueName.Contains("/") || valueName.Contains("."))
        {
            throw new ArgumentException($"注册表值名称包含非法字符: {valueName}");
        }

        if (valueData.Length > 1024)
        {
            throw new ArgumentException("注册表值数据长度超过限制");
        }
    }

    #endregion

    #region 功能方法

    private void ToggleGameMode()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleGameMode();
            UpdateStatusText();
        }
    }

    private void UpdateStatusText()
    {
        Console.WriteLine($"状态更新: 鼠标穿透={_isClickThroughEnabled}, 游戏模式={(DataContext as ViewModels.MainWindowViewModel)?.GameMode ?? false}");
    }

    private void SetClickThrough(bool enable)
    {
        if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
        {
            int style = GetWindowLong(handle.Handle, GWL_EXSTYLE);
            
            if (enable)
            {
                style |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            }
            else
            {
                style &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLong(handle.Handle, GWL_EXSTYLE, style);
        }
    }

    private void OnMinimizeToTrayClick(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    #endregion
}