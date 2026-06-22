using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;

namespace ComputerCompanion.Services;

public interface IChartService {}
public class ChartService : IChartService {}

public static class ChartColors
{
    public static readonly SKColor CpuColor = new SKColor(78, 205, 196);
    public static readonly SKColor GpuColor = new SKColor(162, 155, 254);
    public static readonly SKColor MemoryColor = new SKColor(253, 121, 168);
    public static readonly SKColor FpsColor = new SKColor(118, 185, 0);
}

