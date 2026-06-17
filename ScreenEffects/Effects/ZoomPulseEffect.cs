using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ZoomPulseEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        float scale = 1f + 0.18f * (float)Math.Sin(elapsedSec * Math.PI * 2.0);

        canvas.Save();
        canvas.Translate(dest.MidX, dest.MidY);
        canvas.Scale(scale);
        canvas.Translate(-dest.MidX, -dest.MidY);
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest);
        canvas.Restore();
    }
}
