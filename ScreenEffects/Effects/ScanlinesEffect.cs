using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ScanlinesEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest);

        using var paint = new SKPaint { Color = new SKColor(0, 0, 0, 120) };
        float step = dest.Height / (frame.Height / 2f); // 1 line per 2 source pixels

        for (float y = dest.Top; y < dest.Bottom; y += step * 2)
            canvas.DrawRect(new SKRect(dest.Left, y, dest.Right, y + step), paint);
    }
}
