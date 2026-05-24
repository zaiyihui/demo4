using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

/// <summary>
/// 硬件监控服务，负责采集和管理系统硬件信息
/// 支持 CPU、GPU、内存、网络、磁盘、电池等多种硬件监控
/// </summary>
public class HardwareMonitorService : IDisposable
{
    #region 私有字段

    private Computer? _computer;
    private System.Timers.Timer? _timer;
    
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
    private int _currentHostIndex = 0;

    #endregion

    #region 公共属性

    /// <summary>
    /// CPU 使用率（百分比）
    /// </summary>
    public float? CpuUsage { get; private set; }

    /// <summary>
    /// CPU 温度（摄氏度）
    /// </summary>
    public float? CpuTemp { get; private set; }

    /// <summary>
    /// CPU 风扇转速（RPM）
    /// </summary>
    public int? CpuFanSpeed { get; private set; }

    /// <summary>
    /// GPU 使用率（百分比）
    /// </summary>
    public float? GpuUsage { get; private set; }

    /// <summary>
    /// GPU 温度（摄氏度）
    /// </summary>
    public float? GpuTemp { get; private set; }

    /// <summary>
    /// GPU 风扇转速（RPM）
    /// </summary>
    public int? GpuFanSpeed { get; private set; }

    /// <summary>
    /// GPU 显存使用量（GB）
    /// </summary>
    public float? GpuVramUsed { get; private set; }

    /// <summary>
    /// GPU 显存总量（GB）
    /// </summary>
    public float? GpuVramTotal { get; private set; }

    /// <summary>
    /// GPU 时钟频率（MHz）
    /// </summary>
    public float? GpuClock { get; private set; }

    /// <summary>
    /// 内存使用量（GB）
    /// </summary>
    public float? MemoryUsed { get; private set; }

    /// <summary>
    /// 内存总量（GB）
    /// </summary>
    public float? MemoryTotal { get; private set; }

    /// <summary>
    /// 网络上传速率（MB/s）
    /// </summary>
    public float? NetworkUpload { get; private set; }

    /// <summary>
    /// 网络下载速率（MB/s）
    /// </summary>
    public float? NetworkDownload { get; private set; }

    /// <summary>
    /// 磁盘可用空间（GB）
    /// </summary>
    public float? DiskFreeSpace { get; private set; }

    /// <summary>
    /// 磁盘总空间（GB）
    /// </summary>
    public float? DiskTotalSpace { get; private set; }

    /// <summary>
    /// 电池电量（百分比）
    /// </summary>
    public float? BatteryLevel { get; private set; }

    /// <summary>
    /// 是否正在充电
    /// </summary>
    public bool? IsCharging { get; private set; }

    /// <summary>
    /// 是否检测到独立显卡
    /// </summary>
    public bool HasGpu => GpuUsage.HasValue;

    /// <summary>
    /// 是否检测到电池
    /// </summary>
    public bool HasBattery => BatteryLevel.HasValue;

    /// <summary>
    /// 当前帧率（FPS）
    /// </summary>
    public float? Fps { get; private set; }

    /// <summary>
    /// 网络延迟（毫秒）
    /// </summary>
    public int? NetworkLatency { get; private set; }

    #endregion

    #region 事件

    /// <summary>
    /// 数据更新事件，每次数据刷新后触发
    /// </summary>
    public event Action? DataUpdated;

    #endregion

    #region 公共方法

    /// <summary>
    /// 启动硬件监控服务
    /// </summary>
    /// <param name="intervalMs">数据刷新间隔（毫秒），默认 1000ms</param>
    public void Start(int intervalMs = 1000)
    {
        // 初始化 LibreHardwareMonitor
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

        // 启动定时刷新
        _timer = new Timer(intervalMs);
        _timer.Elapsed += (_, _) => UpdateData();
        _timer.AutoReset = true;
        _timer.Start();

        // 启动网络延迟测量
        StartPingMonitor();
    }

    /// <summary>
    /// 停止硬件监控服务
    /// </summary>
    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _pingTimer?.Stop();
        _pingTimer?.Dispose();
        _computer?.Close();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    #region FPS 测量方法

    /// <summary>
    /// 标记一个帧（用于FPS计算）
    /// 在渲染循环中调用此方法
    /// </summary>
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

    /// <summary>
    /// 获取当前平滑后的FPS值
    /// </summary>
    /// <returns>FPS值</returns>
    public float? GetSmoothedFps()
    {
        return Fps;
    }

    #endregion

    #region 网络延迟测量方法

    /// <summary>
    /// 启动网络延迟测量
    /// </summary>
    private void StartPingMonitor()
    {
        _pingTimer = new Timer(3000); // 每3秒测量一次
        _pingTimer.Elapsed += (_, _) => _ = MeasureLatencyAsync();
        _pingTimer.AutoReset = true;
        _pingTimer.Start();
        
        // 立即执行第一次测量
        _ = MeasureLatencyAsync();
    }

    /// <summary>
    /// 异步测量网络延迟
    /// </summary>
    private async Task MeasureLatencyAsync()
    {
        try
        {
            var host = _pingHosts[_currentHostIndex];
            _currentHostIndex = (_currentHostIndex + 1) % _pingHosts.Length;

            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 1000);

            if (reply.Status == IPStatus.Success)
            {
                _pingLatencyMs = (int)reply.RoundtripTime;
                NetworkLatency = _pingLatencyMs;
            }
            else
            {
                // 尝试下一个主机
                await MeasureLatencyAsync();
            }
        }
        catch
        {
            // Ping 失败，保持上次值或标记为不可用
            if (_pingLatencyMs < 0)
            {
                NetworkLatency = null;
            }
        }
    }

    #endregion

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新所有硬件数据
    /// </summary>
    private void UpdateData()
    {
        if (_computer != null)
        {
            UpdateHardwareData();
        }
        UpdateNetworkData();
        UpdateDiskData();
        UpdateBatteryData();
        
        DataUpdated?.Invoke();
    }

    /// <summary>
    /// 更新通过 LibreHardwareMonitor 获取的硬件数据
    /// </summary>
    private void UpdateHardwareData()
    {
        if (_computer == null) return;

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            ProcessHardware(hardware);
        }
    }

    /// <summary>
    /// 递归处理硬件设备及其子设备
    /// </summary>
    /// <param name="hardware">硬件设备</param>
    private void ProcessHardware(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            ProcessSensor(sensor, hardware.HardwareType);
        }

        // 递归处理子设备（如 CPU 的核心、GPU 的显存等）
        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            ProcessHardware(subHardware);
        }
    }

    /// <summary>
    /// 处理单个传感器数据
    /// </summary>
    /// <param name="sensor">传感器</param>
    /// <param name="hardwareType">硬件类型</param>
    private void ProcessSensor(ISensor sensor, HardwareType hardwareType)
    {
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

    /// <summary>
    /// 处理负载传感器数据
    /// </summary>
    private void ProcessLoadSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Cpu && sensor.Name == "CPU Total")
            CpuUsage = sensor.Value;
        else if (IsGpuType(hardwareType) && sensor.Name == "GPU Core")
            GpuUsage = sensor.Value;
    }

    /// <summary>
    /// 处理温度传感器数据
    /// </summary>
    private void ProcessTemperatureSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Cpu)
            CpuTemp = sensor.Value;
        else if (IsGpuType(hardwareType) && sensor.Name == "GPU Core")
            GpuTemp = sensor.Value;
    }

    /// <summary>
    /// 处理风扇传感器数据
    /// </summary>
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

    /// <summary>
    /// 处理数据传感器（内存/显存）数据
    /// </summary>
    private void ProcessDataSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Memory)
        {
            if (sensor.Name.Contains("Memory Used"))
                MemoryUsed = sensor.Value / 1024; // 转换为 GB
            else if (sensor.Name.Contains("Memory Available"))
                MemoryTotal = (sensor.Value + MemoryUsed.GetValueOrDefault() * 1024) / 1024;
        }
        else if (IsGpuType(hardwareType))
        {
            if (sensor.Name.Contains("VRAM Used"))
                GpuVramUsed = sensor.Value / 1024; // 转换为 GB
            else if (sensor.Name.Contains("VRAM Total"))
                GpuVramTotal = sensor.Value / 1024;
        }
    }

    /// <summary>
    /// 处理电源传感器数据
    /// </summary>
    private void ProcessPowerSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (hardwareType == HardwareType.Battery && sensor.Name.Contains("Charge"))
            BatteryLevel = sensor.Value;
    }

    /// <summary>
    /// 处理时钟频率传感器数据
    /// </summary>
    private void ProcessClockSensor(ISensor sensor, HardwareType hardwareType)
    {
        if (IsGpuType(hardwareType) && sensor.Name.Contains("GPU Core"))
            GpuClock = sensor.Value;
    }

    /// <summary>
    /// 判断是否为 GPU 类型
    /// </summary>
    private bool IsGpuType(HardwareType hardwareType)
    {
        return hardwareType == HardwareType.GpuNvidia || 
               hardwareType == HardwareType.GpuAmd || 
               hardwareType == HardwareType.GpuIntel;
    }

    /// <summary>
    /// 更新网络数据
    /// </summary>
    private void UpdateNetworkData()
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

    /// <summary>
    /// 更新磁盘数据（取第一个固定磁盘）
    /// </summary>
    private void UpdateDiskData()
    {
        var drives = System.IO.DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == System.IO.DriveType.Fixed);

        foreach (var drive in drives)
        {
            DiskFreeSpace = (float)drive.TotalFreeSpace / (1024 * 1024 * 1024);
            DiskTotalSpace = (float)drive.TotalSize / (1024 * 1024 * 1024);
            break; // 只取第一个磁盘
        }
    }

    /// <summary>
    /// 更新电池数据（仅 Windows）
    /// </summary>
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
        catch
        {
            BatteryLevel = null;
            IsCharging = null;
        }
    }

    /// <summary>
    /// 获取电池电量百分比
    /// </summary>
    private float GetBatteryLevel()
    {
        SYSTEM_POWER_STATUS status = new SYSTEM_POWER_STATUS();
        if (GetSystemPowerStatus(ref status))
        {
            return status.BatteryLifePercent;
        }
        return -1;
    }

    /// <summary>
    /// 获取充电状态
    /// </summary>
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

    /// <summary>
    /// Windows 电源状态结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;        // 交流电源状态：0=断开，1=连接，255=未知
        public byte BatteryFlag;         // 电池状态标志
        public byte BatteryLifePercent;  // 电池剩余电量（0-100）
        public byte Reserved1;           // 保留
        public uint BatteryLifeTime;     // 剩余电池使用时间（秒）
        public uint BatteryFullLifeTime; // 充满电后的总时间（秒）
    }

    /// <summary>
    /// 获取系统电源状态
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS lpSystemPowerStatus);

    #endregion
}
