using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class InvertColorsEffect : IScreenEffect
{
    private static readonly float[] Matrix =
    [
        -1,  0,  0, 0, 255,
         0, -1,  0, 0, 255,
         0,  0, -1, 0, 255,
         0,  0,  0, 1,   0,
    ];

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateColorMatrix(Matrix) };
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest, paint);
    }
}
