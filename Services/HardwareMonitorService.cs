using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Timers;

namespace ComputerCompanion.Services;

public class HardwareMonitorService
{
    private Computer? _computer;
    private Timer? _timer;
    
    public float? CpuUsage { get; private set; }
    public float? CpuTemp { get; private set; }
    public int? CpuFanSpeed { get; private set; }
    
    public float? GpuUsage { get; private set; }
    public float? GpuTemp { get; private set; }
    public int? GpuFanSpeed { get; private set; }
    public float? GpuVramUsed { get; private set; }
    public float? GpuVramTotal { get; private set; }
    
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

    public event Action? DataUpdated;

    private long _lastBytesReceived = 0;
    private long _lastBytesSent = 0;
    private DateTime _lastNetworkUpdate = DateTime.Now;

    public void Start(int intervalMs = 1000)
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

        _timer = new Timer(intervalMs);
        _timer.Elapsed += (_, _) => UpdateData();
        _timer.AutoReset = true;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _computer?.Close();
    }

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

    private void UpdateHardwareData()
    {
        if (_computer == null) return;

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            ProcessHardware(hardware);
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
        switch (sensor.SensorType)
        {
            case SensorType.Load:
                if (hardwareType == HardwareType.Cpu && sensor.Name == "CPU Total")
                    CpuUsage = sensor.Value;
                else if (hardwareType == HardwareType.GpuNvidia || 
                         hardwareType == HardwareType.GpuAmd || 
                         hardwareType == HardwareType.GpuIntel)
                {
                    if (sensor.Name == "GPU Core")
                        GpuUsage = sensor.Value;
                }
                break;

            case SensorType.Temperature:
                if (hardwareType == HardwareType.Cpu)
                    CpuTemp = sensor.Value;
                else if (hardwareType == HardwareType.GpuNvidia || 
                         hardwareType == HardwareType.GpuAmd || 
                         hardwareType == HardwareType.GpuIntel)
                {
                    if (sensor.Name == "GPU Core")
                        GpuTemp = sensor.Value;
                }
                break;

            case SensorType.Fan:
                if (hardwareType == HardwareType.Cpu || hardwareType == HardwareType.Motherboard)
                {
                    if (sensor.Name.Contains("CPU") || sensor.Name == "Fan")
                        CpuFanSpeed = (int?)sensor.Value;
                }
                else if (hardwareType == HardwareType.GpuNvidia || 
                         hardwareType == HardwareType.GpuAmd || 
                         hardwareType == HardwareType.GpuIntel)
                {
                    if (sensor.Name.Contains("GPU") || sensor.Name == "Fan")
                        GpuFanSpeed = (int?)sensor.Value;
                }
                break;

            case SensorType.Data:
                if (hardwareType == HardwareType.Memory)
                {
                    if (sensor.Name.Contains("Memory Used"))
                        MemoryUsed = sensor.Value / 1024;
                    else if (sensor.Name.Contains("Memory Available"))
                        MemoryTotal = (sensor.Value + MemoryUsed.GetValueOrDefault() * 1024) / 1024;
                }
                else if (hardwareType == HardwareType.GpuNvidia || 
                         hardwareType == HardwareType.GpuAmd || 
                         hardwareType == HardwareType.GpuIntel)
                {
                    if (sensor.Name.Contains("VRAM Used"))
                        GpuVramUsed = sensor.Value / 1024;
                    else if (sensor.Name.Contains("VRAM Total"))
                        GpuVramTotal = sensor.Value / 1024;
                }
                break;

            case SensorType.Power:
                if (hardwareType == HardwareType.Battery)
                {
                    if (sensor.Name.Contains("Charge"))
                        BatteryLevel = sensor.Value;
                }
                break;
        }
    }

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
            NetworkDownload = (float)((totalBytesReceived - _lastBytesReceived) / elapsed / 1024 / 1024);
            NetworkUpload = (float)((totalBytesSent - _lastBytesSent) / elapsed / 1024 / 1024);
        }

        _lastBytesReceived = totalBytesReceived;
        _lastBytesSent = totalBytesSent;
        _lastNetworkUpdate = now;
    }

    private void UpdateDiskData()
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
