using Avalonia.Data.Converters;
using Avalonia.Media;
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
