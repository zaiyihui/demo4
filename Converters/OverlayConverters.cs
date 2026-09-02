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



public class SelectedMetricToBackgroundConverter : IValueConverter
{
    public static readonly SelectedMetricToBackgroundConverter Instance = new();

    private static readonly LinearGradientBrush SelectedBrush = new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#80ffffff"), 0),
            new GradientStop(Color.Parse("#68ffffff"), 0.2),
            new GradientStop(Color.Parse("#55ffffff"), 0.4),
            new GradientStop(Color.Parse("#45ffffff"), 0.6),
            new GradientStop(Color.Parse("#38ffffff"), 0.8),
            new GradientStop(Color.Parse("#2dffffff"), 1)
        }
    };

    private static readonly LinearGradientBrush UnselectedBrush = new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#40ffffff"), 0),
            new GradientStop(Color.Parse("#35ffffff"), 0.25),
            new GradientStop(Color.Parse("#2affffff"), 0.5),
            new GradientStop(Color.Parse("#20ffffff"), 0.75),
            new GradientStop(Color.Parse("#18ffffff"), 1)
        }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            return selectedMetric == cardName ? SelectedBrush : UnselectedBrush;
        }
        return UnselectedBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToBorderConverter : IValueConverter
{
    public static readonly SelectedMetricToBorderConverter Instance = new();

    private static readonly LinearGradientBrush SelectedBrush = new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#80ffffff"), 0),
            new GradientStop(Color.Parse("#65ffffff"), 0.3),
            new GradientStop(Color.Parse("#50ffffff"), 0.5),
            new GradientStop(Color.Parse("#3dffffff"), 0.7),
            new GradientStop(Color.Parse("#2dffffff"), 1)
        }
    };

    private static readonly LinearGradientBrush UnselectedBrush = new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#35ffffff"), 0),
            new GradientStop(Color.Parse("#28ffffff"), 0.3),
            new GradientStop(Color.Parse("#20ffffff"), 0.5),
            new GradientStop(Color.Parse("#18ffffff"), 0.7),
            new GradientStop(Color.Parse("#10ffffff"), 1)
        }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            return selectedMetric == cardName ? SelectedBrush : UnselectedBrush;
        }
        return UnselectedBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectedMetricToShadowConverter : IValueConverter
{
    public static readonly SelectedMetricToShadowConverter Instance = new();

    private static readonly BoxShadows SelectedShadow = BoxShadows.Parse("0 20 60 #50000000, 0 8 25 #35000000");
    private static readonly BoxShadows UnselectedShadow = BoxShadows.Parse("0 8 20 #20000000, 0 3 8 #10000000");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedMetric && parameter is string cardName)
        {
            return selectedMetric == cardName ? SelectedShadow : UnselectedShadow;
        }
        return UnselectedShadow;
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

public class FpsValueConverter : IValueConverter
{
    public static readonly FpsValueConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double fps)
        {
            if (fps < 0)
            {
                return "N/A";
            }
            return $"{fps:F0}";
        }
        return "N/A";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
