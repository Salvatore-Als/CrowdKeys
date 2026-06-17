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
            ScreenEffectType.Mirror              => "Miroir Horizontal",
            ScreenEffectType.ShuffleQuadrants    => "Split Écran x2",
            ScreenEffectType.ShuffleQuadrants4   => "Split Écran x4",
            ScreenEffectType.Blur                => "Flou (Blur)",
            ScreenEffectType.Drunk               => "Screen Shake",
            ScreenEffectType.FlipVertical        => "Flip Vertical",
            ScreenEffectType.InvertColors        => "Inversion Couleurs",
            ScreenEffectType.Grayscale           => "Noir & Blanc",
            ScreenEffectType.Pixelate            => "Pixelisé",
            ScreenEffectType.ZoomIn              => "Zoom x1.6",
            ScreenEffectType.ChromaticAberration => "Aberration RGB",
            ScreenEffectType.Glitch              => "Glitch",
            ScreenEffectType.Scanlines           => "Scanlines CRT",
            ScreenEffectType.ZoomPulse           => "Zoom Pulsé",
            _                                    => value.ToString()
        } : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
