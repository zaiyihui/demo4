using System;

namespace ComputerCompanion.Models;

public class ChartPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
    
    public double X => Time.ToOADate();
    public double Y => Value;
}

public class MetricDataPointViewModel
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}