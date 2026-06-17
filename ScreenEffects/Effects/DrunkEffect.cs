using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class DrunkEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        float angle = (float)(Math.Sin(elapsedSec * 1.5) * 13.0);
        float cx = dest.MidX;
        float cy = dest.MidY;

        canvas.Save();
        canvas.Translate(cx, cy);
        canvas.RotateDegrees(angle);
        canvas.Scale(1.09f);
        canvas.Translate(-cx, -cy);
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest);
        canvas.Restore();
    }
}
