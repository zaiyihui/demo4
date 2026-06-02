using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ComputerCompanion.Services;

public class TrayIconService : IDisposable
{
    private bool _isDisposed;
    private NotifyIcon? _notifyIcon;
    private Thread? _messageLoopThread;
    private bool _messageLoopRunning;

    public TrayIconService()
    {
    }

    public void Initialize()
    {
        if (_isDisposed || _notifyIcon != null)
            return;

        Program.Log("[托盘] 正在初始化托盘图标服务");

        try
        {
            _messageLoopThread = new Thread(StartMessageLoop)
            {
                Name = "TrayIconMessageLoop",
                IsBackground = true
            };
            _messageLoopThread.SetApartmentState(ApartmentState.STA);
            _messageLoopThread.Start();

            Program.Log("[托盘] 托盘图标初始化成功");
        }
        catch (Exception ex)
        {
            Program.Log($"[托盘] 初始化失败: {ex.Message}（程序将继续运行，仅无托盘图标）");
        }
    }

    private void StartMessageLoop()
    {
        try
        {
            Application.EnableVisualStyles();

            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "电脑伴侣"
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("显示主窗口", null, (s, e) => SafeRaise(ShowMainWindow));
            menu.Items.Add("-");
            menu.Items.Add("退出程序", null, (s, e) => SafeRaise(ExitApplication));

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    SafeRaise(ShowMainWindow);
            };

            _messageLoopRunning = true;
            Program.Log("[托盘] 消息循环已启动");
            Application.Run();
        }
        catch (Exception ex)
        {
            Program.Log($"[托盘] 消息循环异常: {ex.Message}");
        }
    }

    private void SafeRaise(EventHandler? handler)
    {
        try
        {
            handler?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Program.Log($"[托盘] 事件处理失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
        catch { }

        try
        {
            if (_messageLoopRunning)
            {
                _messageLoopRunning = false;
                Application.ExitThread();
            }
        }
        catch { }

        Program.Log("[托盘] 已释放");
    }

    public event EventHandler? ShowMainWindow;
    public event EventHandler? ExitApplication;
}