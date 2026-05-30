using System;
using System.Windows.Forms;
using System.Drawing;

namespace ComputerCompanion.Services;

public class TrayIconService : IDisposable
{
    private bool _isDisposed;
    private NotifyIcon? _notifyIcon;

    public TrayIconService(SettingsService settingsService)
    {
    }

    public void Initialize()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(SystemIcons.Application, 40, 40),
                Visible = true,
                Text = "电脑伴侣"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示主窗口", null, (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => ExitApplication?.Invoke(this, EventArgs.Empty));
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowMainWindow?.Invoke(this, EventArgs.Empty);
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化托盘图标失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _notifyIcon?.Dispose();
    }

    public event EventHandler? ShowMainWindow;
    public event EventHandler? ExitApplication;
}