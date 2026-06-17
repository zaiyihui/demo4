using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading.Tasks;
using System.Threading;

namespace ComputerCompanion.Services;

public class HardwareMonitorService : IHardwareMonitorService
{
    private Computer? _computer;
    private System.Timers.Timer? _dataTimer;
    private bool _isRunning;
    
    private long _frameCount = 0;
    private long _lastFpsUpdateTime = 0;
    private float _currentFps = 0;
    private readonly long _ticksPerSecond = TimeSpan.TicksPerSecond;
    private bool _fpsInitialized = false;

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

    public float? DiskFreeSpace { get; private set; }
    public float? DiskTotalSpace { get; private set; }

    public float? Fps { get; private set; }

    public bool HasGpu => GpuUsage.HasValue;

    public event Action? DataUpdated;

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
                IsNetworkEnabled = false,
                IsControllerEnabled = true,
                IsBatteryEnabled = false
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
    }

    private async void OnDataTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await Task.Run(() => UpdateData());
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新硬件数据失败: {ex.Message}");
        }
    }

    private float _lastCpuUsage = 0;
    private float _lastGpuUsage = 0;
    private float _lastMemoryUsage = 0;
    private const float UpdateThreshold = 0.5f;

    private bool ShouldUpdateUI(float currentValue, float lastValue)
    {
        return Math.Abs(currentValue - lastValue) > UpdateThreshold;
    }

    public void Stop()
    {
        _isRunning = false;
        
        _dataTimer?.Stop();
        _dataTimer?.Dispose();
        
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

    private void UpdateData()
    {
        try
        {
            UpdateHardwareData();
            UpdateDiskData();

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
            var previousMemoryUsed = MemoryUsed;
            var previousMemoryTotal = MemoryTotal;
            MemoryUsed = null;
            MemoryTotal = null;
            
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                ProcessHardware(hardware);
            }
            
            if (!MemoryTotal.HasValue || MemoryTotal.Value <= 0)
            {
                MemoryTotal = GetTotalPhysicalMemory();
                Program.Log($"[硬件] 使用系统API获取内存总量: {MemoryTotal?.ToString("F2")} GB");
            }
            
            if (MemoryTotal.HasValue && !MemoryUsed.HasValue)
            {
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
            return;
        
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
            var cpuTempNames = new[] { "CPU Package", "Core (Tctl/Tdie)", "CPU Core", "Core #1", "CPU", "Package" };
            bool isPrioritySensor = cpuTempNames.Any(name => 
                sensor.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                sensor.Name.Contains(name));
            
            if (isPrioritySensor || !CpuTemp.HasValue)
            {
                CpuTemp = sensor.Value;
            }
        }
        else if (IsGpuType(hardwareType))
        {
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
            var sensorName = sensor.Name.ToLowerInvariant();
            
            if (sensorName.Contains("used"))
            {
                MemoryUsed = sensor.Value / 1024;
                Program.Log($"[硬件] 内存已使用: {MemoryUsed?.ToString("F2")} GB (传感器: {sensor.Name})");
            }
            else if (sensorName.Contains("available"))
            {
                var memoryAvailable = sensor.Value / 1024;
                if (MemoryUsed.HasValue)
                {
                    MemoryTotal = MemoryUsed.Value + memoryAvailable;
                }
            }
            else if (sensorName.Contains("total"))
            {
                MemoryTotal = sensor.Value / 1024;
            }
            
            if (MemoryUsed.HasValue && !MemoryTotal.HasValue)
            {
                try
                {
                    var totalPhysicalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                    MemoryTotal = (float)(totalPhysicalMemory / (1024.0 * 1024.0 * 1024.0));
                }
                catch
                {
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

    private float GetTotalPhysicalMemory()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcInfo.TotalAvailableMemoryBytes;
            return (float)(totalMemory / (1024.0 * 1024.0 * 1024.0));
        }
        catch
        {
            try
            {
                var processMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                return (float)(processMemory * 4 / (1024.0 * 1024.0 * 1024.0));
            }
            catch
            {
                return 16.0f;
            }
        }
    }

    private float GetUsedPhysicalMemory()
    {
        try
        {
            var availableMemory = GetAvailablePhysicalMemory();
            var totalMemory = GetTotalPhysicalMemory();
            
            if (availableMemory.HasValue && totalMemory > 0)
            {
                return totalMemory - availableMemory.Value;
            }
            
            var processMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            return (float)(processMemory / (1024.0 * 1024.0 * 1024.0));
        }
        catch
        {
            return 0.0f;
        }
    }

    private float? GetAvailablePhysicalMemory()
    {
        try
        {
            MEMORYSTATUSEX status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            
            if (GlobalMemoryStatusEx(ref status))
            {
                return (float)(status.ullAvailPhys / (1024.0 * 1024.0 * 1024.0));
            }
        }
        catch
        {
        }
        
        return null;
    }

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
}