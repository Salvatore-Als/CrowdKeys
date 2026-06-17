using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ZoomInEffect : IScreenEffect
{
    private readonly float _scale;

    public ZoomInEffect(float scale = 1.6f) => _scale = scale;

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        canvas.Save();
        canvas.Translate(dest.MidX, dest.MidY);
        canvas.Scale(_scale);
        canvas.Translate(-dest.MidX, -dest.MidY);
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest);
        canvas.Restore();
    }
}
