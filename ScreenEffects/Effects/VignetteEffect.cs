using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class VignetteEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest);

        var center = new SKPoint(dest.MidX, dest.MidY);
        float radius = Math.Max(dest.Width, dest.Height) * 0.75f;

        using var shader = SKShader.CreateRadialGradient(
            center, radius,
            [SKColors.Transparent, new SKColor(0, 0, 0, 230)],
            [0.35f, 1.0f],
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(dest, paint);
    }
}
