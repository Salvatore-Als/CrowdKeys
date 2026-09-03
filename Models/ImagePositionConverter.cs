using System.Globalization;
using Avalonia.Data.Converters;
using CrowdKeys.Localization;

namespace CrowdKeys.Models;

public class ImagePositionConverter : IValueConverter, IMultiValueConverter
{
    public static readonly ImagePositionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ImagePosition p ? p switch
        {
            ImagePosition.TopLeft      => Loc.Instance["Position_TopLeft"],
            ImagePosition.TopCenter    => Loc.Instance["Position_TopCenter"],
            ImagePosition.TopRight     => Loc.Instance["Position_TopRight"],
            ImagePosition.MiddleLeft   => Loc.Instance["Position_MiddleLeft"],
            ImagePosition.Center       => Loc.Instance["Position_Center"],
            ImagePosition.MiddleRight  => Loc.Instance["Position_MiddleRight"],
            ImagePosition.BottomLeft   => Loc.Instance["Position_BottomLeft"],
            ImagePosition.BottomCenter => Loc.Instance["Position_BottomCenter"],
            ImagePosition.BottomRight  => Loc.Instance["Position_BottomRight"],
            _                          => value.ToString()
        } : value?.ToString();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        Convert(values.Count > 0 ? values[0] : null, targetType, parameter, culture);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
