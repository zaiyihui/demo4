using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Timers;

namespace ComputerCompanion.Services;

public class HardwareMonitorService
{
    private Computer? _computer;
    private Timer? _timer;
    
    public float? CpuUsage { get; private set; }
    public float? CpuTemp { get; private set; }
    public float? GpuUsage { get; private set; }
    public float? GpuTemp { get; private set; }
    public float? MemoryUsed { get; private set; }
    public float? MemoryTotal { get; private set; }

    public event Action? DataUpdated;

    public void Start(int intervalMs = 1000)
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };
        _computer.Open();

        _timer = new Timer(intervalMs);
        _timer.Elapsed += (_, _) => UpdateData();
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void UpdateData()
    {
        if (_computer == null) return;

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    CpuUsage = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total")?.Value;
                    CpuTemp = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value;
                    break;

                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                    GpuUsage = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")?.Value;
                    GpuTemp = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "GPU Core")?.Value;
                    break;

                case HardwareType.Memory:
                    var memUsed = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Used"))?.Value;
                    var memTotal = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Available"))?.Value;
                    if (memUsed.HasValue && memTotal.HasValue)
                    {
                        MemoryUsed = memUsed / 1024;
                        MemoryTotal = (memTotal + memUsed) / 1024;
                    }
                    break;
            }

            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Update();
                if (subHardware.HardwareType == HardwareType.GpuNvidia || 
                    subHardware.HardwareType == HardwareType.GpuAmd ||
                    subHardware.HardwareType == HardwareType.GpuIntel)
                {
                    GpuUsage = subHardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")?.Value;
                    GpuTemp = subHardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "GPU Core")?.Value;
                }
            }
        }

        DataUpdated?.Invoke();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _computer?.Close();
    }
}
