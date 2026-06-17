using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public interface IScreenEffect
{
    void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest);
}
