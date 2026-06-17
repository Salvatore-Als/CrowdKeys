using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class GrayscaleEffect : IScreenEffect
{
    private static readonly float[] Matrix =
    [
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0,      0,      0,      1, 0,
    ];

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateColorMatrix(Matrix) };
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest, paint);
    }
}
