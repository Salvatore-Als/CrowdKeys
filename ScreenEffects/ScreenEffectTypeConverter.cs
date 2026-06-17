using System.Globalization;
using Avalonia.Data.Converters;
using CrowdKeys.Models;

namespace CrowdKeys.ScreenEffects;

public class ScreenEffectTypeConverter : IValueConverter
{
    public static readonly ScreenEffectTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ScreenEffectType t ? t switch
        {
            ScreenEffectType.Mirror           => "Miroir",
            ScreenEffectType.ShuffleQuadrants => "Quadrants mélangés",
            ScreenEffectType.Blur             => "Myopie (blur)",
            ScreenEffectType.Drunk            => "Mode bourré",
            _                                 => value.ToString()
        } : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
