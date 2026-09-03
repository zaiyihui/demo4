using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private System.Timers.Timer? _fpsTimer;
    private bool _isRunning;
    
    private long _frameCount = 0;
    private long _lastFpsUpdateTime = 0;
    private float _currentFps = 0;
    private readonly long _ticksPerSecond = TimeSpan.TicksPerSecond;
    private bool _fpsInitialized = false;
    
    private readonly Queue<float> _fpsHistory = new();
    private const int MaxFpsHistorySize = 600;
    public float? Fps1PercentLow { get; private set; }
    public float? Fps01PercentLow { get; private set; }
    
    private Process? _activeGameProcess;
    private List<string> _gameProcessNames = new()
    {
        "cs2", "csgo", "dota2", "pubg", "fortnite", "apex", "valorant", 
        "overwatch", "warframe", "eldenring", "cyberpunk2077", "godofwar",
        "hogwartslegacy", "starfield", "deadspace", "residentevil4",
        "bf2042", "callofduty", "warzone", "haloinfinite", "destiny2",
        "genshinimpact", "honkaistarrail", "leagueoflegends", "lol",
        "worldofwarcraft", "wow", "fifa", "nba2k", "mlbtheshow",
        "steam", "epicgameslauncher", "origin", "uplay", "battlenet",
        "goggalaxy", "rockstargames", "ubisoftconnect"
    };
    
    private long _lastProcessCheckTime = 0;
    private const long ProcessCheckIntervalMs = 2000;
    
    private float? _smoothedFps;
    private const float FpsSmoothingFactor = 0.2f;

    // ETW-based real FPS monitor (PresentMon 同款方案)
    private readonly FpsMonitorService? _fpsMonitor;
    public float? FrameTimeMs => _fpsMonitor?.FrameTimeMs;
    public bool IsRealFpsAvailable => _fpsMonitor?.IsEtwAvailable == true;
    
    private bool IsGameRunning => _activeGameProcess != null && !_activeGameProcess.HasExited;
    
    private bool IsGraphicsActivityDetected => GpuUsage.HasValue && GpuUsage.Value > 10;
    
    private bool ShouldDisplayFps => IsGameRunning || IsGraphicsActivityDetected;
    
    public void AddGameProcess(string processName)
    {
        if (!string.IsNullOrWhiteSpace(processName) && !_gameProcessNames.Contains(processName.ToLowerInvariant()))
        {
            _gameProcessNames.Add(processName.ToLowerInvariant());
            Program.Log($"[硬件] 添加游戏进程: {processName}");
        }
    }
    
    public void RemoveGameProcess(string processName)
    {
        var removed = _gameProcessNames.Remove(processName.ToLowerInvariant());
        if (removed)
        {
            Program.Log($"[硬件] 移除游戏进程: {processName}");
        }
    }
    
    public List<string> GetGameProcessNames()
    {
        return _gameProcessNames.ToList();
    }
    
    private void CheckGameProcesses()
    {
        if (_activeGameProcess != null && !_activeGameProcess.HasExited)
            return;

        var now = DateTime.UtcNow.Ticks;
        var elapsedMs = (now - _lastProcessCheckTime) / TimeSpan.TicksPerMillisecond;
        
        if (elapsedMs < ProcessCheckIntervalMs)
        {
            return;
        }
        
        _lastProcessCheckTime = now;

        try
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var processName = process.ProcessName.ToLowerInvariant();
                    if (_gameProcessNames.Contains(processName))
                    {
                        _activeGameProcess = process;
                        Program.Log($"[硬件] 检测到游戏进程: {process.ProcessName} (PID: {process.Id})");
                        // 将游戏进程 PID 传给 ETW FPS 监控
                        _fpsMonitor?.SetTargetProcess(process.Id);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.Log($"[硬件] 访问进程 {process.ProcessName} 失败: {ex.Message}");
                }
            }
            
            _activeGameProcess = null;
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 检测游戏进程失败: {ex.Message}");
        }
    }
    
    private float SmoothFps(float rawFps)
    {
        if (_smoothedFps == null)
            _smoothedFps = rawFps;
        
        _smoothedFps = _smoothedFps.Value * (1 - FpsSmoothingFactor) + rawFps * FpsSmoothingFactor;
        return _smoothedFps.Value;
    }
    
    public void ResetFpsStatistics()
    {
        _fpsHistory.Clear();
        _smoothedFps = null;
        Fps1PercentLow = null;
        Fps01PercentLow = null;
        Program.Log("[硬件] FPS统计已重置");
    }

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

    private float _lastCpuUsage = 0;
    private float _lastGpuUsage = 0;
    private float _lastCpuTemp = 0;
    private float _lastGpuTemp = 0;
    private const float UpdateThreshold = 0.5f;
    private const float TempUpdateThreshold = 1.0f;
    
    private long _diskUpdateCounter = 0;
    private const long DiskUpdateInterval = 10;

    private static readonly HashSet<string> _cpuTempNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CPU Package", "Core (Tctl/Tdie)", "CPU Core", "Core #1", "CPU", "Package"
    };

    private static readonly HashSet<string> _gpuLoadNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "GPU Core", "GPU Load"
    };

    private static readonly HashSet<string> _gpuTempNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "GPU Core", "GPU Temperature"
    };

    public void Start(int intervalMs = 1000)
    {
        if (_isRunning)
            return;

        _isRunning = true;
        Program.Log("[硬件] 开始初始化硬件监控服务");

        // 启动 ETW 真实 FPS 监控
        try
        {
            _fpsMonitor?.Start();
            if (_fpsMonitor?.IsEtwAvailable == true)
                Program.Log("[硬件] ETW 真实 FPS 监控已启用");
            else
                Program.Log("[硬件] ETW 不可用，回退到渲染帧率统计");
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] ETW FPS 监控启动失败: {ex.Message}");
        }

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

        try
        {
            _fpsTimer = new System.Timers.Timer(16);
            _fpsTimer.Elapsed += OnFpsTimerElapsed;
            _fpsTimer.AutoReset = true;
            _fpsTimer.Start();
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] FPS定时器启动失败: {ex.Message}");
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

    private void OnFpsTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            CheckGameProcesses();
            MarkFrame();
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] FPS更新失败: {ex.Message}");
        }
    }

    private bool ShouldUpdateUI(float currentValue, float lastValue, float threshold = UpdateThreshold)
    {
        return Math.Abs(currentValue - lastValue) > threshold;
    }

    public void Stop()
    {
        _isRunning = false;

        _dataTimer?.Stop();
        _dataTimer?.Dispose();

        _fpsTimer?.Stop();
        _fpsTimer?.Dispose();

        _fpsMonitor?.Stop();

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
        // ETW 真实 FPS 优先
        if (_fpsMonitor?.IsEtwAvailable == true && _fpsMonitor.CurrentFps.HasValue)
        {
            var realFps = _fpsMonitor.CurrentFps.Value;
            if (realFps > 0)
            {
                Fps = realFps;
                if (_fpsMonitor.Fps1PercentLow.HasValue)
                    Fps1PercentLow = _fpsMonitor.Fps1PercentLow;
            }
            return;
        }

        // 回退到原有渲染帧率统计
        if (!ShouldDisplayFps)
        {
            if (Fps.HasValue)
            {
                Fps = null;
                ResetFpsStatistics();
            }
            return;
        }

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
            
            if (_currentFps > 0 && _currentFps < 300)
            {
                var smoothedFps = SmoothFps(_currentFps);
                Fps = smoothedFps;
                UpdateFpsPercentiles(_currentFps);
            }
            
            _frameCount = 0;
            _lastFpsUpdateTime = currentTime;
        }
    }

    private void UpdateFpsPercentiles(float fps)
    {
        _fpsHistory.Enqueue(fps);
        
        while (_fpsHistory.Count > MaxFpsHistorySize)
        {
            _fpsHistory.Dequeue();
        }
        
        if (_fpsHistory.Count >= 100)
        {
            var sortedFps = _fpsHistory.OrderBy(f => f).ToArray();
            
            var index1Percent = (int)(_fpsHistory.Count * 0.01);
            Fps1PercentLow = sortedFps[Math.Min(index1Percent, sortedFps.Length - 1)];
            
            var index01Percent = (int)(_fpsHistory.Count * 0.001);
            Fps01PercentLow = sortedFps[Math.Min(index01Percent, sortedFps.Length - 1)];
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
            bool needUpdate = UpdateHardwareData();
            
            _diskUpdateCounter++;
            if (_diskUpdateCounter >= DiskUpdateInterval)
            {
                UpdateDiskData();
                _diskUpdateCounter = 0;
            }

            if (needUpdate)
            {
                RaiseDataUpdated();
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新数据失败: {ex.Message}");
        }
    }



    private bool UpdateHardwareData()
    {
        if (_computer == null) return false;

        bool needUpdate = false;
        
        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                needUpdate |= ProcessHardware(hardware);
            }
            
            if (!MemoryTotal.HasValue || MemoryTotal.Value <= 0)
            {
                MemoryTotal = GetTotalPhysicalMemory();
            }
            
            if (MemoryTotal.HasValue && !MemoryUsed.HasValue)
            {
                MemoryUsed = GetUsedPhysicalMemory();
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[硬件] 更新硬件数据失败: {ex.Message}");
        }

        return needUpdate;
    }

    private bool ProcessHardware(IHardware hardware)
    {
        bool needUpdate = false;

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value == null)
                continue;

            needUpdate |= ProcessSensor(sensor, hardware.HardwareType);
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            needUpdate |= ProcessHardware(subHardware);
        }

        return needUpdate;
    }

    private bool ProcessSensor(ISensor sensor, HardwareType hardwareType)
    {
        bool needUpdate = false;
        
        switch (sensor.SensorType)
        {
            case SensorType.Load:
                needUpdate |= ProcessLoadSensor(sensor, hardwareType);
                break;
            case SensorType.Temperature:
                needUpdate |= ProcessTemperatureSensor(sensor, hardwareType);
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
        
        return needUpdate;
    }

    private bool ProcessLoadSensor(ISensor sensor, HardwareType hardwareType)
    {
        bool needUpdate = false;
        
        if (hardwareType == HardwareType.Cpu && sensor.Name == "CPU Total")
        {
            if (ShouldUpdateUI((float)sensor.Value, _lastCpuUsage))
            {
                CpuUsage = sensor.Value;
                _lastCpuUsage = (float)sensor.Value;
                needUpdate = true;
            }
        }
        else if (IsGpuType(hardwareType) && _gpuLoadNames.Contains(sensor.Name))
        {
            if (ShouldUpdateUI((float)sensor.Value, _lastGpuUsage))
            {
                GpuUsage = sensor.Value;
                _lastGpuUsage = (float)sensor.Value;
                needUpdate = true;
            }
        }
        
        return needUpdate;
    }

    private bool ProcessTemperatureSensor(ISensor sensor, HardwareType hardwareType)
    {
        bool needUpdate = false;
        
        if (hardwareType == HardwareType.Cpu)
        {
            bool isPrioritySensor = _cpuTempNames.Any(name => 
                sensor.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                sensor.Name.Contains(name));
            
            if (isPrioritySensor || !CpuTemp.HasValue)
            {
                if (!CpuTemp.HasValue || ShouldUpdateUI((float)sensor.Value, _lastCpuTemp, TempUpdateThreshold))
                {
                    CpuTemp = sensor.Value;
                    _lastCpuTemp = (float)sensor.Value;
                    needUpdate = true;
                }
            }
        }
        else if (IsGpuType(hardwareType))
        {
            if (_gpuTempNames.Contains(sensor.Name) || !GpuTemp.HasValue)
            {
                if (!GpuTemp.HasValue || ShouldUpdateUI((float)sensor.Value, _lastGpuTemp, TempUpdateThreshold))
                {
                    GpuTemp = sensor.Value;
                    _lastGpuTemp = (float)sensor.Value;
                    needUpdate = true;
                }
            }
        }
        
        return needUpdate;
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

    private void RaiseDataUpdated()
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