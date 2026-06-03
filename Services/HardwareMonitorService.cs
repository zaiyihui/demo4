using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

public class HardwareMonitorService : IHardwareMonitorService
{
    #region 私有字段

    private Computer? _computer;
    private System.Timers.Timer? _dataTimer;
    private bool _isRunning;
    
    // 网络统计相关
    private long _lastBytesReceived = 0;
    private long _lastBytesSent = 0;
    private DateTime _lastNetworkUpdate = DateTime.Now;
    
    // FPS 测量相关
    private long _frameCount = 0;
    private long _lastFpsUpdateTime = 0;
    private float _currentFps = 0;
    private readonly long _ticksPerSecond = TimeSpan.TicksPerSecond;
    private bool _fpsInitialized = false;
    
    // 网络延迟测量相关
    private int _pingLatencyMs = -1;
    private System.Timers.Timer? _pingTimer;
    private readonly string[] _pingHosts = { "223.5.5.5", "119.29.29.29", "8.8.8.8", "1.1.1.1" };

    #endregion

    #region 公共属性

    public float? CpuUsage { get; private set; }
    public float? CpuTemp { get; private set; }
    public int? CpuFanSpeed { get; private set; }

    public float? GpuUsage { get; private set; }
    public float? GpuTemp { get; private set; }
    public int? GpuFanSpeed { get; private set; }
    public float? GpuVramUsed { get; private set; }
    public float? GpuVramTotal { get; private set; }
    public float? GpuClock { get; private set; }

    public float? MemoryUsed { get; private set; }
    public float? MemoryTotal { get; private set; }

    public float? NetworkUpload { get; private set; }
    public float? NetworkDownload { get; private set; }

    public float? DiskFreeSpace { get; private set; }
    public float? DiskTotalSpace { get; private set; }

    public float? BatteryLevel { get; private set; }
    public bool? IsCharging { get; private set; }

    public bool HasGpu => GpuUsage.HasValue;
    public bool HasBattery => BatteryLevel.HasValue;

    public float? Fps { get; private set; }
    public int? NetworkLatency { get; private set; }

    #endregion

    #region 事件

    public event Action? DataUpdated;

    #endregion

    #region 公共方法

    public void Start(int intervalMs = 1000)
    {
        if (_isRunning)
            return;

        _isRunning = true;
        Program.Log("[硬件] 开始初始化硬件监控服务");

        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true,
                IsControllerEnabled = true,
                IsBatteryEnabled = true
            };
            _computer.Open();
            Program.Log("[硬件] LibreHardwareMonitor 已成功启动");
        }
        catch (System.Security.SecurityException ex)
        {
            Program.Log($"[硬件] 权限不足: {ex.Message}（提示：以管理员身份运行可获得更完整的硬件数据）");
        }
        catch (System.UnauthorizedAccessException ex)
        {
            Program.Log($"[硬件] 拒绝访问: {ex.Message}（提示：以管理员身份运行可获得更完整的硬件数据）");
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 初始化失败: {ex.Message}（程序将继续运行，仅硬件数据不可用）");
        }

        try
        {
            _dataTimer = new System.Timers.Timer(intervalMs);
            _dataTimer.Elapsed += OnDataTimerElapsed;
            _dataTimer.AutoReset = true;
            _dataTimer.Start();
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 定时器启动失败: {ex.Message}");
        }

        StartPingMonitor();
    }

    private async void OnDataTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // 使用Task.Run避免阻塞定时器线程
            await Task.Run(() => UpdateData());
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新硬件数据失败: {ex.Message}");
        }
    }

    // 差分更新：记录上一次的数据值，避免不必要的UI更新
    private float _lastCpuUsage = 0;
    private float _lastGpuUsage = 0;
    private float _lastMemoryUsage = 0;
    private const float UpdateThreshold = 0.5f; // 0.5%的变化才触发更新

    private bool ShouldUpdateUI(float currentValue, float lastValue)
    {
        return Math.Abs(currentValue - lastValue) > UpdateThreshold;
    }

    public void Stop()
    {
        _isRunning = false;
        
        _dataTimer?.Stop();
        _dataTimer?.Dispose();
        _pingTimer?.Stop();
        _pingTimer?.Dispose();
        
        try
        {
            _computer?.Close();
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 关闭硬件监控失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    #region FPS 测量方法

    public void MarkFrame()
    {
        if (!_fpsInitialized)
        {
            _lastFpsUpdateTime = DateTime.UtcNow.Ticks;
            _fpsInitialized = true;
            return;
        }

        _frameCount++;
        var currentTime = DateTime.UtcNow.Ticks;
        var elapsedTicks = currentTime - _lastFpsUpdateTime;

        if (elapsedTicks >= _ticksPerSecond)
        {
            _currentFps = (float)(_frameCount * _ticksPerSecond) / elapsedTicks;
            Fps = _currentFps;
            _frameCount = 0;
            _lastFpsUpdateTime = currentTime;
        }
    }

    public float? GetSmoothedFps()
    {
        return Fps;
    }

    #endregion

    #region 网络延迟测量方法

    private void StartPingMonitor()
    {
        _pingTimer = new System.Timers.Timer(3000);
        _pingTimer.Elapsed += async (_, _) => await MeasureLatencyAsync();
        _pingTimer.AutoReset = true;
        _pingTimer.Start();
        
        _ = MeasureLatencyAsync();
    }

    private async Task MeasureLatencyAsync()
    {
        foreach (var host in _pingHosts)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 1000);

                if (reply.Status == IPStatus.Success)
                {
                    _pingLatencyMs = (int)reply.RoundtripTime;
                    NetworkLatency = _pingLatencyMs;
                    return;
                }
            }
            catch (Exception ex)
            {
                Program.Log($"[硬件] Ping {host} 失败: {ex.Message}");
            }
        }

        if (_pingLatencyMs < 0)
        {
            NetworkLatency = null;
        }
    }

    #endregion

    #endregion

    #region 私有方法

    private void UpdateData()
    {
        try
        {
            // 更新原始硬件数据
            UpdateHardwareData();
            UpdateNetworkData();
            UpdateDiskData();
            UpdateBatteryData();

            // 差分更新：只有关键指标变化超过阈值时才触发UI更新
            bool needUpdate = false;
            
            if (CpuUsage.HasValue && ShouldUpdateUI(CpuUsage.Value, _lastCpuUsage))
            {
                needUpdate = true;
                _lastCpuUsage = CpuUsage.Value;
            }
            
            if (GpuUsage.HasValue && ShouldUpdateUI(GpuUsage.Value, _lastGpuUsage))
            {
                needUpdate = true;
                _lastGpuUsage = GpuUsage.Value;
            }
            
            if (MemoryUsed.HasValue && MemoryTotal.HasValue)
            {
                var currentMemoryUsage = (MemoryUsed.Value / MemoryTotal.Value) * 100;
                if (ShouldUpdateUI(currentMemoryUsage, _lastMemoryUsage))
                {
                    needUpdate = true;
                    _lastMemoryUsage = currentMemoryUsage;
                }
            }

            if (needUpdate)
            {
                // 使用线程安全的方式触发事件
                var handler = Volatile.Read(ref DataUpdated);
                if (handler != null)
                {
                    try
                    {
                        handler();
                    }
                    catch (Exception ex)
                    {
                        Program.Log($"[硬件] DataUpdated事件处理时发生错误: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新数据失败: {ex.Message}");
        }
    }

    private void UpdateHardwareData()
    {
        if (_computer == null) return;

        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                ProcessHardware(hardware);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新硬件数据失败: {ex.Message}");
        }
    }

    private void ProcessHardware(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            ProcessSensor(sensor, hardware.HardwareType);
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            ProcessHardware(subHardware);
        }
    }

    private void ProcessSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (sensor.Value == null)
        {
            return;
        }
        
        switch (sensor.SensorType)
        {
            case SensorType.Load:
                ProcessLoadSensor(sensor, hardwareType);
                break;

            case SensorType.Temperature:
                ProcessTemperatureSensor(sensor, hardwareType);
                break;

            case SensorType.Fan:
                ProcessFanSensor(sensor, hardwareType);
                break;

            case SensorType.Data:
                ProcessDataSensor(sensor, hardwareType);
                break;

            case SensorType.Power:
                ProcessPowerSensor(sensor, hardwareType);
                break;

            case SensorType.Clock:
                ProcessClockSensor(sensor, hardwareType);
                break;
        }
    }

    private void ProcessLoadSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Cpu && sensor.Name == "CPU Total")
            CpuUsage = sensor.Value;
        else if (IsGpuType(hardwareType) && sensor.Name == "GPU Core")
            GpuUsage = sensor.Value;
    }

    private void ProcessTemperatureSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Cpu)
            CpuTemp = sensor.Value;
        else if (IsGpuType(hardwareType) && sensor.Name == "GPU Core")
            GpuTemp = sensor.Value;
    }

    private void ProcessFanSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Cpu || hardwareType == HardwareType.Motherboard)
        {
            if (sensor.Name.Contains("CPU") || sensor.Name == "Fan")
                CpuFanSpeed = (int?)sensor.Value;
        }
        else if (IsGpuType(hardwareType))
        {
            if (sensor.Name.Contains("GPU") || sensor.Name == "Fan")
                GpuFanSpeed = (int?)sensor.Value;
        }
    }

    private void ProcessDataSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Memory)
        {
            if (sensor.Name.Contains("Memory Used"))
                MemoryUsed = sensor.Value / 1024;
            else if (sensor.Name.Contains("Memory Available"))
                MemoryTotal = sensor.Value / 1024 + MemoryUsed.GetValueOrDefault();
            else if (sensor.Name.Contains("Memory Total"))
                MemoryTotal = sensor.Value / 1024;
        }
        else if (IsGpuType(hardwareType))
        {
            if (sensor.Name.Contains("VRAM Used"))
                GpuVramUsed = sensor.Value / 1024;
            else if (sensor.Name.Contains("VRAM Total"))
                GpuVramTotal = sensor.Value / 1024;
            else if (sensor.Name.Contains("Dedicated Video Memory"))
                GpuVramTotal = sensor.Value / 1024;
        }
    }

    private void ProcessPowerSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Battery && sensor.Name.Contains("Charge"))
            BatteryLevel = sensor.Value;
    }

    private void ProcessClockSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (IsGpuType(hardwareType) && sensor.Name.Contains("GPU Core"))
            GpuClock = sensor.Value;
    }

    private bool IsGpuType(HardwareType hardwareType)
    {
        return hardwareType == HardwareType.GpuNvidia || 
               hardwareType == HardwareType.GpuAmd || 
               hardwareType == HardwareType.GpuIntel;
    }

    private void UpdateNetworkData()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && 
                           !n.Description.Contains("Loopback") &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (var iface in interfaces)
            {
                var stats = iface.GetIPStatistics();
                totalBytesReceived += stats.BytesReceived;
                totalBytesSent += stats.BytesSent;
            }

            var now = DateTime.Now;
            var elapsed = (now - _lastNetworkUpdate).TotalSeconds;

            if (elapsed > 0 && _lastBytesReceived > 0)
            {
                var downloadDiff = totalBytesReceived - _lastBytesReceived;
                var uploadDiff = totalBytesSent - _lastBytesSent;
                
                if (downloadDiff >= 0 && uploadDiff >= 0)
                {
                    NetworkDownload = Math.Max(0, (float)(downloadDiff / elapsed / 1024 / 1024));
                    NetworkUpload = Math.Max(0, (float)(uploadDiff / elapsed / 1024 / 1024));
                }
            }

            _lastBytesReceived = totalBytesReceived;
            _lastBytesSent = totalBytesSent;
            _lastNetworkUpdate = now;
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新网络数据失败: {ex.Message}");
        }
    }

    private void UpdateDiskData()
    {
        try
        {
            var drives = System.IO.DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == System.IO.DriveType.Fixed);

            foreach (var drive in drives)
            {
                DiskFreeSpace = (float)drive.TotalFreeSpace / (1024 * 1024 * 1024);
                DiskTotalSpace = (float)drive.TotalSize / (1024 * 1024 * 1024);
                break;
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新磁盘数据失败: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新电池数据失败: {ex.Message}");
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

    #endregion

    #region Win32 API 定义

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

    #endregion
}