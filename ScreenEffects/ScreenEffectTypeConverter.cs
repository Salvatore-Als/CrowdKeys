using System.Globalization;
using Avalonia.Data.Converters;
using CrowdKeys.Localization;
using CrowdKeys.Models;

namespace CrowdKeys.ScreenEffects;

public class ScreenEffectTypeConverter : IValueConverter, IMultiValueConverter
{
    public static readonly ScreenEffectTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ScreenEffectType t ? t switch
        {
            ScreenEffectType.Mirror              => Loc.Instance["Effect_Mirror"],
            ScreenEffectType.ShuffleQuadrants    => Loc.Instance["Effect_Split2"],
            ScreenEffectType.ShuffleQuadrants4   => Loc.Instance["Effect_Split4"],
            ScreenEffectType.Blur                => Loc.Instance["Effect_Blur"],
            ScreenEffectType.Drunk               => Loc.Instance["Effect_Shake"],
            ScreenEffectType.FlipVertical        => Loc.Instance["Effect_Flip"],
            ScreenEffectType.InvertColors        => Loc.Instance["Effect_Invert"],
            ScreenEffectType.Grayscale           => Loc.Instance["Effect_Grayscale"],
            ScreenEffectType.Pixelate            => Loc.Instance["Effect_Pixelate"],
            ScreenEffectType.ZoomIn              => Loc.Instance["Effect_ZoomIn"],
            ScreenEffectType.ChromaticAberration => Loc.Instance["Effect_Chroma"],
            ScreenEffectType.Glitch              => Loc.Instance["Effect_Glitch"],
            ScreenEffectType.Scanlines           => Loc.Instance["Effect_Scanlines"],
            ScreenEffectType.ZoomPulse           => Loc.Instance["Effect_ZoomPulse"],
            _                                    => value.ToString()
        } : value?.ToString();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        Convert(values.Count > 0 ? values[0] : null, targetType, parameter, culture);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
