using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ComputerCompanion.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ComputerCompanion.Services;

public interface IChartService
{
    ChartPoint GetChartPoint();
    void ReturnChartPoint(ChartPoint point);
    MetricDataPointViewModel GetMetricDataPoint();
    void ReturnMetricDataPoint(MetricDataPointViewModel point);
    ISeries[] CreateMetricSeries(string name, SKColor color);
}

public class ChartService : IChartService
{
    private readonly ObjectPool<ChartPoint> _chartPointPool = new(
        () => new ChartPoint(),
        point => { point.Time = default; point.Value = 0; },
        maxPoolSize: 100);

    private readonly ObjectPool<MetricDataPointViewModel> _metricPointPool = new(
        () => new MetricDataPointViewModel(),
        point => { point.Timestamp = default; point.Value = 0; },
        maxPoolSize: 100);

    public ChartPoint GetChartPoint()
    {
        return _chartPointPool.Get();
    }

    public void ReturnChartPoint(ChartPoint point)
    {
        _chartPointPool.Return(point);
    }

    public MetricDataPointViewModel GetMetricDataPoint()
    {
        return _metricPointPool.Get();
    }

    public void ReturnMetricDataPoint(MetricDataPointViewModel point)
    {
        _metricPointPool.Return(point);
    }

    public ISeries[] CreateMetricSeries(string name, SKColor color)
    {
        return new ISeries[]
        {
            new LineSeries<double>
            {
                Name = name,
                Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.8
            }
        };
    }
}

public static class ChartColors
{
    public static readonly SKColor CpuColor = new SKColor(78, 205, 196);
    public static readonly SKColor GpuColor = new SKColor(162, 155, 254);
    public static readonly SKColor MemoryColor = new SKColor(253, 121, 168);
    public static readonly SKColor FpsColor = new SKColor(118, 185, 0);
}

public class ObjectPool<T> where T : class
{
    private readonly Func<T> _createFunc;
    private readonly Action<T> _resetAction;
    private readonly int _maxPoolSize;
    private readonly Queue<T> _pool = new();
    private int _count;

    public ObjectPool(Func<T> createFunc, Action<T> resetAction, int maxPoolSize = 100)
    {
        _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        _resetAction = resetAction ?? throw new ArgumentNullException(nameof(resetAction));
        _maxPoolSize = maxPoolSize;
    }

    public T Get()
    {
        lock (_pool)
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }
        }

        return _createFunc();
    }

    public void Return(T item)
    {
        if (item == null) return;

        _resetAction(item);

        lock (_pool)
        {
            if (_pool.Count < _maxPoolSize)
            {
                _pool.Enqueue(item);
            }
        }
    }

    public int Count => Volatile.Read(ref _count);
}