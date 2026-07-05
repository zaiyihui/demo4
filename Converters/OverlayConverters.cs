using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using ComputerCompanion.Models;
using System;
using System.Globalization;

namespace ComputerCompanion.Converters;

public static class BoolConverters
{
    public static IValueConverter ToEnabledColor { get; } = new FuncValueConverter<bool, IBrush>(
        value => value ? new SolidColorBrush(Color.Parse("#27ae60")) : new SolidColorBrush(Color.Parse("#e74c3c")));

    public static IValueConverter ToEnabledText { get; } = new FuncValueConverter<bool, string>(
        value => value ? "已启用" : "已禁用");
}

public class OverlayPositionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is OverlayPosition position && parameter is string positionName)
        {
            return positionName switch
            {
                "TopLeft" => position == OverlayPosition.TopLeft,
                "TopRight" => position == OverlayPosition.TopRight,
                "BottomLeft" => position == OverlayPosition.BottomLeft,
                "BottomRight" => position == OverlayPosition.BottomRight,
                _ => false
            };
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string positionName)
        {
            return positionName switch
            {
                "TopLeft" => OverlayPosition.TopLeft,
                "TopRight" => OverlayPosition.TopRight,
                "BottomLeft" => OverlayPosition.BottomLeft,
                "BottomRight" => OverlayPosition.BottomRight,
                _ => OverlayPosition.TopRight
            };
        }
        return OverlayPosition.TopRight;
    }
}

public static class OverlayPositionConverters
{
    public static readonly IValueConverter ToDisplayName = new FuncValueConverter<OverlayPosition, string>(
        position => position switch
        {
            OverlayPosition.TopLeft => "左上",
            OverlayPosition.TopRight => "右上",
            OverlayPosition.BottomLeft => "左下",
            OverlayPosition.BottomRight => "右下",
            _ => "未知"
        });

    public static readonly IValueConverter IsTopLeft = new OverlayPositionConverter();
    public static readonly IValueConverter IsTopRight = new OverlayPositionConverter();
    public static readonly IValueConverter IsBottomLeft = new OverlayPositionConverter();
    public static readonly IValueConverter IsBottomRight = new OverlayPositionConverter();
}

public class StringToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                return new SolidColorBrush(Color.Parse(colorString));
            }
            catch
            {
                return new SolidColorBrush(Colors.White);
            }
        }
        return new SolidColorBrush(Colors.White);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToColorConverterStatic : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                return Color.Parse(colorString);
            }
            catch
            {
                return Colors.White;
            }
        }
        return Colors.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RecordingIconConverter : IValueConverter
{
    public static readonly RecordingIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "⏹" : "⏺";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RecordingTextConverter : IValueConverter
{
    public static readonly RecordingTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "停止" : "录制";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToBackgroundConverter : IValueConverter
{
    public static readonly SelectedMetricToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            var isSelected = selectedMetric == cardName;
            var brush = new LinearGradientBrush();
            brush.StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative);
            brush.EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative);
            
            if (isSelected)
            {
                brush.GradientStops.Add(new GradientStop(Color.Parse("#18ffffff"), 0));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#10ffffff"), 0.3));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#0cffffff"), 0.6));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#08ffffff"), 1));
            }
            else
            {
                brush.GradientStops.Add(new GradientStop(Color.Parse("#0dffffff"), 0));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#09ffffff"), 0.3));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#06ffffff"), 0.6));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#04ffffff"), 1));
            }
            
            return brush;
        }
        return new SolidColorBrush(Color.Parse("#08ffffff"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToBorderConverter : IValueConverter
{
    public static readonly SelectedMetricToBorderConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            var isSelected = selectedMetric == cardName;
            var brush = new LinearGradientBrush();
            brush.StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative);
            brush.EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative);
            
            if (isSelected)
            {
                brush.GradientStops.Add(new GradientStop(Color.Parse("#60ffffff"), 0));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#50ffffff"), 0.5));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#40ffffff"), 1));
            }
            else
            {
                brush.GradientStops.Add(new GradientStop(Color.Parse("#30ffffff"), 0));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#25ffffff"), 0.5));
                brush.GradientStops.Add(new GradientStop(Color.Parse("#20ffffff"), 1));
            }
            
            return brush;
        }
        return new SolidColorBrush(Color.Parse("#30ffffff"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToShadowConverter : IValueConverter
{
    public static readonly SelectedMetricToShadowConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            var isSelected = selectedMetric == cardName;
            return isSelected 
                ? "0 20 60 0 #50000000, 0 8 25 0 #35000000"
                : "0 8 20 0 #20000000, 0 3 8 0 #10000000";
        }
        return "0 8 20 0 #20000000";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToOpacityConverter : IValueConverter
{
    public static readonly SelectedMetricToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            return selectedMetric == cardName ? 1.0 : 0.85;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToScaleConverter : IValueConverter
{
    public static readonly SelectedMetricToScaleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            return selectedMetric == cardName ? 1.02 : 1.0;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
