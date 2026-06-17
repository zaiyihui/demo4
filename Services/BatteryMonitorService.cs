using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace ComputerCompanion.Services;

public class BatteryMonitorService : IBatteryMonitorService
{
    private System.Timers.Timer? _timer;
    private bool _isRunning;
    private bool _isDisposed;

    public float? BatteryLevel { get; private set; }
    public bool? IsCharging { get; private set; }
    public bool HasBattery => BatteryLevel.HasValue;

    public event Action? BatteryUpdated;

    public void Start(int intervalMs = 2000)
    {
        if (_isRunning || _isDisposed)
            return;

        _isRunning = true;
        Program.Log("[电池] 启动电池监控服务");

        try
        {
            if (OperatingSystem.IsWindows())
            {
                _timer = new System.Timers.Timer(intervalMs);
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();
                
                UpdateBatteryData();
            }
            else
            {
                Program.Log("[电池] 非Windows系统，电池监控不可用");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[电池] 定时器启动失败: {ex.Message}");
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            UpdateBatteryData();
        }
        catch (Exception ex)
        {
            Program.Log($"[电池] 更新电池数据失败: {ex.Message}");
        }
    }

    private void UpdateBatteryData()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                BatteryLevel = GetBatteryLevel();
                IsCharging = GetIsCharging();
            }
            else
            {
                BatteryLevel = null;
                IsCharging = null;
            }
            
            BatteryUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Program.Log($"[电池] 更新电池数据失败: {ex.Message}");
            BatteryLevel = null;
            IsCharging = null;
        }
    }

    private float GetBatteryLevel()
    {
        SYSTEM_POWER_STATUS status = new SYSTEM_POWER_STATUS();
        if (GetSystemPowerStatus(ref status))
        {
            return status.BatteryLifePercent;
        }
        return -1;
    }

    private bool GetIsCharging()
    {
        SYSTEM_POWER_STATUS status = new SYSTEM_POWER_STATUS();
        if (GetSystemPowerStatus(ref status))
        {
            return status.ACLineStatus == 1;
        }
        return false;
    }

    public void Stop()
    {
        _isRunning = false;
        
        _timer?.Stop();
        _timer?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
        GC.SuppressFinalize(this);
        Program.Log("[电池] 电池监控服务已释放");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte Reserved1;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS lpSystemPowerStatus);
}