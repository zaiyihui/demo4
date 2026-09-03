using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComputerCompanion.Services;

/// <summary>
/// GPU 风扇曲线控制服务（基于 LibreHardwareMonitor 的 Control 传感器）
/// </summary>
public class FanControlService : IDisposable
{
    private Computer? _computer;
    private readonly List<ISensor> _controlSensors = new();
    private readonly List<FanCurvePoint> _defaultCurve = new()
    {
        new FanCurvePoint { Temperature = 30, FanPercent = 30 },
        new FanCurvePoint { Temperature = 50, FanPercent = 45 },
        new FanCurvePoint { Temperature = 65, FanPercent = 60 },
        new FanCurvePoint { Temperature = 75, FanPercent = 80 },
        new FanCurvePoint { Temperature = 85, FanPercent = 100 },
    };

    private bool _disposed;

    public bool IsFanControlAvailable { get; private set; }
    public List<FanCurvePoint> CurrentCurve { get; private set; } = new();

    /// <summary>
    /// 接收 HardwareMonitorService 的 Computer 对象引用，遍历 GPU 硬件查找 Control 传感器。
    /// </summary>
    public void Initialize(Computer computer)
    {
        _computer = computer;
        _controlSensors.Clear();
        IsFanControlAvailable = false;

        if (_computer == null)
            return;

        foreach (var hardware in _computer.Hardware)
        {
            if (!IsGpuType(hardware.HardwareType))
                continue;

            hardware.Update();
            CollectControlSensors(hardware);

            // 递归子硬件（部分 GPU 会把子设备挂在 SubHardware 上）
            foreach (var sub in hardware.SubHardware)
            {
                sub.Update();
                CollectControlSensors(sub);
            }
        }

        IsFanControlAvailable = _controlSensors.Count > 0;
    }

    private void CollectControlSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType == SensorType.Control)
                _controlSensors.Add(sensor);
        }
    }

    private static bool IsGpuType(HardwareType type)
    {
        return type == HardwareType.GpuNvidia ||
               type == HardwareType.GpuAmd ||
               type == HardwareType.GpuIntel;
    }

    public List<FanCurvePoint> GetDefaultCurve() =>
        new List<FanCurvePoint>(_defaultCurve);

    /// <summary>
    /// 如果已应用过曲线则返回当前曲线，否则返回默认曲线。
    /// </summary>
    public List<FanCurvePoint> GetCurrentCurve()
    {
        if (CurrentCurve.Count > 0)
            return new List<FanCurvePoint>(CurrentCurve);
        return GetDefaultCurve();
    }

    /// <summary>
    /// 根据当前 GPU 温度在曲线上插值计算目标风扇百分比，并应用到 Control 传感器。
    /// </summary>
    public bool ApplyFanCurve(List<FanCurvePoint> curve)
    {
        if (!IsFanControlAvailable || _computer == null || curve == null || curve.Count == 0)
            return false;

        // 按温度升序排序，便于插值
        var sorted = curve.OrderBy(p => p.Temperature).ToList();

        float? gpuTemp = null;
        foreach (var hardware in _computer.Hardware)
        {
            if (!IsGpuType(hardware.HardwareType))
                continue;

            hardware.Update();
            gpuTemp ??= GetGpuTemperature(hardware);
            foreach (var sub in hardware.SubHardware)
            {
                sub.Update();
                gpuTemp ??= GetGpuTemperature(sub);
            }
        }

        if (!gpuTemp.HasValue)
            return false;

        float targetPercent = Interpolate(sorted, gpuTemp.Value);

        foreach (var control in _controlSensors)
        {
            var ctrl = control.Control;
            if (ctrl == null)
                continue;

            // SetSoftware 会接管控制（关闭自动模式）并设置软件控制值
            ctrl.SetSoftware(targetPercent);
        }

        CurrentCurve = new List<FanCurvePoint>(sorted);
        return true;
    }

    private static float? GetGpuTemperature(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                return (float)sensor.Value;
        }
        return null;
    }

    /// <summary>
    /// 在曲线上按温度插值计算风扇百分比。
    /// 低于最低点取最低点值，高于最高点取最高点值。
    /// </summary>
    private static float Interpolate(List<FanCurvePoint> curve, float temperature)
    {
        if (temperature <= curve[0].Temperature)
            return curve[0].FanPercent;

        if (temperature >= curve[^1].Temperature)
            return curve[^1].FanPercent;

        for (int i = 0; i < curve.Count - 1; i++)
        {
            var p1 = curve[i];
            var p2 = curve[i + 1];
            if (temperature >= p1.Temperature && temperature <= p2.Temperature)
            {
                float t = (temperature - p1.Temperature) /
                          (p2.Temperature - p1.Temperature);
                return p1.FanPercent + t * (p2.FanPercent - p1.FanPercent);
            }
        }

        return curve[^1].FanPercent;
    }

    /// <summary>
    /// 将所有 Control 传感器设回自动模式。
    /// </summary>
    public void ResetToAuto()
    {
        foreach (var control in _controlSensors)
        {
            var ctrl = control.Control;
            ctrl?.SetDefault();
        }

        CurrentCurve.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            ResetToAuto();
        }
        catch { }

        _controlSensors.Clear();
        _computer = null;
        _disposed = true;
    }
}

public class FanCurvePoint
{
    public float Temperature { get; set; }
    public float FanPercent { get; set; }
}
