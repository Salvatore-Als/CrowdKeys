using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class BlurEffect : IScreenEffect
{
    private readonly float _sigma;

    public BlurEffect(float sigma = 14f) => _sigma = sigma;

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(_sigma, _sigma)
        };
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest, paint);
    }
}
