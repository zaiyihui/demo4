using System;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Services;

public interface IGlobalHotkeyService : IDisposable
{
    event Action? ToggleOverlay;
    event Action? SwitchViewMode;
    
    bool RegisterHotkeys();
    void UnregisterHotkeys();
}

public class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_TOGGLE_OVERLAY = 1001;
    private const int HOTKEY_ID_SWITCH_VIEW = 1002;
    
    private IntPtr _hwnd;
    private bool _isRegistered;

    public event Action? ToggleOverlay;
    public event Action? SwitchViewMode;

    public GlobalHotkeyService()
    {
    }

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public bool RegisterHotkeys()
    {
        if (_isRegistered)
            return true;

        if (_hwnd == IntPtr.Zero)
        {
            Program.Log("[热键] 窗口句柄未初始化，跳过热键注册");
            return false;
        }

        try
        {
            var altR = RegisterHotKey(_hwnd, HOTKEY_ID_TOGGLE_OVERLAY, 
                (uint)(KeyModifiers.Alt), (uint)Keys.R);
            
            var altShiftR = RegisterHotKey(_hwnd, HOTKEY_ID_SWITCH_VIEW, 
                (uint)(KeyModifiers.Alt | KeyModifiers.Shift), (uint)Keys.R);

            _isRegistered = altR && altShiftR;
            
            if (_isRegistered)
            {
                Program.Log("[热键] 全局热键注册成功: Alt+R(切换悬浮窗), Alt+Shift+R(切换视图)");
            }
            else
            {
                Program.Log("[热键] 全局热键注册失败，可能被其他程序占用");
            }
            
            return _isRegistered;
        }
        catch (Exception ex)
        {
            Program.Log($"[热键] 注册热键异常: {ex.Message}");
            return false;
        }
    }

    public void UnregisterHotkeys()
    {
        if (!_isRegistered)
            return;

        try
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID_TOGGLE_OVERLAY);
            UnregisterHotKey(_hwnd, HOTKEY_ID_SWITCH_VIEW);
            
            _isRegistered = false;
            
            Program.Log("[热键] 全局热键已注销");
        }
        catch (Exception ex)
        {
            Program.Log($"[热键] 注销热键异常: {ex.Message}");
        }
    }

    public void HandleWindowMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            OnHotkey(id);
        }
    }

    private void OnHotkey(int id)
    {
        try
        {
            switch (id)
            {
                case HOTKEY_ID_TOGGLE_OVERLAY:
                    ToggleOverlay?.Invoke();
                    break;
                case HOTKEY_ID_SWITCH_VIEW:
                    SwitchViewMode?.Invoke();
                    break;
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[热键] 处理热键事件异常: {ex.Message}");
        }
    }

    public void Dispose()
    {
        UnregisterHotkeys();
        GC.SuppressFinalize(this);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [Flags]
    private enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }

    private enum Keys
    {
        R = 0x52
    }
}
