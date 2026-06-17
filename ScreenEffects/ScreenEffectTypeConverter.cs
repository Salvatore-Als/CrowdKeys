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
            ScreenEffectType.Mirror             => "Miroir",
            ScreenEffectType.ShuffleQuadrants   => "Quadrants mélangés x2",
            ScreenEffectType.ShuffleQuadrants4  => "Quadrants mélangés x4",
            ScreenEffectType.ShuffleQuadrants8  => "Quadrants mélangés x8",
            ScreenEffectType.ShuffleQuadrants16 => "Quadrants mélangés x16",
            ScreenEffectType.ShuffleQuadrants32 => "Quadrants mélangés x32",
            ScreenEffectType.ShuffleQuadrants64 => "Quadrants mélangés x64",
            ScreenEffectType.Blur               => "Blur",
            ScreenEffectType.Drunk              => "Screen Shaking",
            _                                   => value.ToString()
        } : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
