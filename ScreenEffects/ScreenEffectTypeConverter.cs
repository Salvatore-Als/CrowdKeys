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
            ScreenEffectType.Mirror              => "Dimension Miroir",
            ScreenEffectType.ShuffleQuadrants    => "Cerveau Fendu",
            ScreenEffectType.ShuffleQuadrants4   => "Cerveau Explosé",
            ScreenEffectType.Blur                => "Myopie Sévère",
            ScreenEffectType.Drunk               => "0.8 g/L",
            ScreenEffectType.FlipVertical        => "Anti-Gravité",
            ScreenEffectType.InvertColors        => "Film Négatif",
            ScreenEffectType.Grayscale           => "Années 50",
            ScreenEffectType.Pixelate            => "Mode Minecraft",
            ScreenEffectType.ZoomIn              => "Trop Près",
            ScreenEffectType.ChromaticAberration => "Écran Mort",
            ScreenEffectType.Glitch              => "Corruption",
            ScreenEffectType.Scanlines           => "Vieille Télé",
            ScreenEffectType.ZoomPulse           => "Palpitations",
            _                                    => value.ToString()
        } : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
