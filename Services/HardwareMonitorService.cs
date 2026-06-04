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
            // 重置内存数据，确保每次更新都能正确计算
            var previousMemoryUsed = MemoryUsed;
            var previousMemoryTotal = MemoryTotal;
            MemoryUsed = null;
            MemoryTotal = null;
            
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                ProcessHardware(hardware);
            }
            
            // 备用方案：如果传感器无法提供内存数据，使用系统API
            if (!MemoryTotal.HasValue || MemoryTotal.Value <= 0)
            {
                MemoryTotal = GetTotalPhysicalMemory();
                Program.Log($"[硬件] 使用系统API获取内存总量: {MemoryTotal?.ToString("F2")} GB");
            }
            
            // 如果有总量但没有已使用量，尝试计算
            if (MemoryTotal.HasValue && !MemoryUsed.HasValue)
            {
                // 使用性能计数器或系统API获取已使用内存
                MemoryUsed = GetUsedPhysicalMemory();
                Program.Log($"[硬件] 使用系统API获取内存已使用: {MemoryUsed?.ToString("F2")} GB");
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
        {
            // 优先查找特定的CPU温度传感器名称（按优先级排序）
            var cpuTempNames = new[] {
                "CPU Package",
                "Core (Tctl/Tdie)",
                "CPU Core",
                "Core #1",
                "CPU",
                "Package"
            };
            
            // 检查是否是优先的传感器名称
            bool isPrioritySensor = cpuTempNames.Any(name => 
                sensor.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                sensor.Name.Contains(name));
            
            // 只有当当前值为空，或者找到优先传感器时才更新
            if (isPrioritySensor || !CpuTemp.HasValue)
            {
                CpuTemp = sensor.Value;
            }
        }
        else if (IsGpuType(hardwareType))
        {
            // GPU温度：优先查找 "GPU Core"，否则使用任何GPU温度传感器
            if (sensor.Name == "GPU Core" || !GpuTemp.HasValue)
            {
                GpuTemp = sensor.Value;
            }
        }
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
            // 改进的内存传感器匹配逻辑
            var sensorName = sensor.Name.ToLowerInvariant();
            
            if (sensorName.Contains("used"))
            {
                // Memory Used: 已使用的内存（GB）
                MemoryUsed = sensor.Value / 1024;
                Program.Log($"[硬件] 内存已使用: {MemoryUsed?.ToString("F2")} GB (传感器: {sensor.Name})");
            }
            else if (sensorName.Contains("available"))
            {
                // Memory Available: 可用内存（GB）
                // 总内存 = 已使用 + 可用
                var memoryAvailable = sensor.Value / 1024;
                if (MemoryUsed.HasValue)
                {
                    MemoryTotal = MemoryUsed.Value + memoryAvailable;
                    Program.Log($"[硬件] 内存总量(计算): {MemoryTotal?.ToString("F2")} GB = 已使用({MemoryUsed?.ToString("F2")}) + 可用({memoryAvailable.ToString("F2")})");
                }
            }
            else if (sensorName.Contains("total"))
            {
                // Memory Total: 直接报告的总内存（GB）
                MemoryTotal = sensor.Value / 1024;
                Program.Log($"[硬件] 内存总量(直接): {MemoryTotal?.ToString("F2")} GB (传感器: {sensor.Name})");
            }
            
            // 如果只有 MemoryUsed 而没有 MemoryTotal，尝试从系统获取总内存
            if (MemoryUsed.HasValue && !MemoryTotal.HasValue)
            {
                try
                {
                    var totalPhysicalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                    MemoryTotal = (float)(totalPhysicalMemory / (1024.0 * 1024.0 * 1024.0));
                    Program.Log($"[硬件] 内存总量(系统): {MemoryTotal?.ToString("F2")} GB (从系统API获取)");
                }
                catch
                {
                    // 如果系统API也失败，使用常见的内存大小作为参考
                    Program.Log("[硬件] 无法获取内存总量，使用估算值");
                }
            }
        }
        else if (IsGpuType(hardwareType))
        {
            var sensorName = sensor.Name.ToLowerInvariant();
            
            if (sensorName.Contains("vram used") || sensorName.Contains("d3d dedicated memory used"))
                GpuVramUsed = sensor.Value / 1024;
            else if (sensorName.Contains("vram total") || sensorName.Contains("dedicated video memory"))
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

    #region 系统内存获取方法

    /// <summary>
    /// 获取物理内存总量（GB）- 使用多种方法确保可靠性
    /// </summary>
    private float GetTotalPhysicalMemory()
    {
        try
        {
            // 方法1: 使用 GC.GetGCMemoryInfo (最可靠)
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcInfo.TotalAvailableMemoryBytes;
            return (float)(totalMemory / (1024.0 * 1024.0 * 1024.0));
        }
        catch
        {
            try
            {
                // 方法2: 使用 Environment.SystemPageSize 和工作集估算
                // 这个方法不太准确，但可以作为备用
                var processMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                // 假设系统内存至少是进程工作集的4倍（粗略估算）
                return (float)(processMemory * 4 / (1024.0 * 1024.0 * 1024.0));
            }
            catch
            {
                // 方法3: 返回一个常见的默认值（16GB）
                return 16.0f;
            }
        }
    }

    /// <summary>
    /// 获取已使用的物理内存（GB）- 使用系统性能数据
    /// </summary>
    private float GetUsedPhysicalMemory()
    {
        try
        {
            // 使用性能计数器获取可用内存，然后计算已使用
            var availableMemory = GetAvailablePhysicalMemory();
            var totalMemory = GetTotalPhysicalMemory();
            
            if (availableMemory.HasValue && totalMemory > 0)
            {
                return totalMemory - availableMemory.Value;
            }
            
            // 备用方案：使用进程工作集估算
            var processMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            return (float)(processMemory / (1024.0 * 1024.0 * 1024.0));
        }
        catch
        {
            return 0.0f;
        }
    }

    /// <summary>
    /// 获取可用的物理内存（GB）
    /// </summary>
    private float? GetAvailablePhysicalMemory()
    {
        try
        {
            // 使用 Windows API 获取可用内存
            MEMORYSTATUSEX status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            
            if (GlobalMemoryStatusEx(ref status))
            {
                return (float)(status.ullAvailPhys / (1024.0 * 1024.0 * 1024.0));
            }
        }
        catch
        {
            // 忽略错误
        }
        
        return null;
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

    // 内存状态结构体
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    #endregion
}