using System;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Services;

/// <summary>
/// 基于 Win32 Shell_NotifyIcon 的系统托盘图标服务（无 WinForms 依赖）
/// </summary>
public class TrayIconService : IDisposable
{
    private bool _isDisposed;
    private IntPtr _hwnd;
    private uint _taskId;
    private bool _iconAdded;
    private bool _running;

    public event EventHandler? OpenSettings;

    public TrayIconService()
    {
    }

    public void Initialize()
    {
        if (_isDisposed || _running)
            return;

        _instance = this;
        Program.Log("[托盘] 正在初始化托盘图标服务（Win32 原生）");

        try
        {
            // 保持委托引用以防 GC 回收
            _windowProcDelegate = WindowProcDelegate;

            // 注册隐藏消息窗口的窗口类
            var className = "ComputerCompanionTray";
            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = _windowProcDelegate,
                lpszClassName = className,
                hInstance = Marshal.GetHINSTANCE(typeof(Program).Module)
            };
            RegisterClassEx(ref wc);

            // 创建消息仅接收窗口
            _hwnd = CreateWindowEx(
                0, className, "ComputerCompanionTray", 0,
                0, 0, 0, 0,
                (IntPtr)0xFFFF, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                Program.Log("[托盘] 创建窗口失败");
                return;
            }

            // 添加托盘图标
            _taskId = (uint)DateTime.Now.GetHashCode();
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = _taskId,
                uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP,
                uCallbackMessage = WM_USER_TRAY,
                hIcon = LoadIcon(IntPtr.Zero, IDI_APPLICATION),
                szTip = "电脑伴侣"
            };
            Shell_NotifyIcon(NIM_ADD, ref nid);
            _iconAdded = true;

            _running = true;
            Program.Log("[托盘] 托盘图标初始化成功");
        }
        catch (Exception ex)
        {
            Program.Log($"[托盘] 初始化失败: {ex.Message}（程序将继续运行，仅无托盘图标）");
        }
    }

    private static WndProc? _windowProcDelegate;
    private static WNDCLASSEX _wc;
    private static TrayIconService? _instance;

    private static IntPtr WindowProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_USER_TRAY)
        {
            var mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);
            if (mouseMsg == WM_LBUTTONUP)
            {
                _instance?.RaiseShowMainWindow();
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                _instance?.ShowContextMenu(hwnd);
            }
        }
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private void RaiseShowMainWindow() => SafeRaise(ShowMainWindow);
    private void RaiseOpenSettings() => SafeRaise(OpenSettings);
    private void RaiseExitApplication() => SafeRaise(ExitApplication);

    private void ShowContextMenu(IntPtr hwnd)
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, 1, "显示主窗口");
        AppendMenu(menu, MF_SEPARATOR, 0, null);
        AppendMenu(menu, MF_STRING, 2, "设置");
        AppendMenu(menu, MF_SEPARATOR, 0, null);
        AppendMenu(menu, MF_STRING, 3, "退出程序");

        GetCursorPos(out var pt);
        SetForegroundWindow(hwnd);
        var cmd = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.x, pt.y, 0, hwnd, IntPtr.Zero);
        DestroyMenu(menu);

        switch (cmd)
        {
            case 1: RaiseShowMainWindow(); break;
            case 2: RaiseOpenSettings(); break;
            case 3: RaiseExitApplication(); break;
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

    public event EventHandler? ShowMainWindow;
    public event EventHandler? ExitApplication;

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            if (_iconAdded && _hwnd != IntPtr.Zero)
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hwnd,
                    uID = _taskId
                };
                Shell_NotifyIcon(NIM_DELETE, ref nid);
                _iconAdded = false;
            }
        }
        catch { }

        try
        {
            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
        }
        catch { }

        _running = false;
        Program.Log("[托盘] 已释放");
    }

    #region Win32 P/Invoke

    private const uint WM_USER_TRAY = 0x8000;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_TIP = 0x00000004;
    private const int IDI_APPLICATION = 32512;
    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;

    private delegate IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, int lpIconName);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags,
        int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    #endregion
}
