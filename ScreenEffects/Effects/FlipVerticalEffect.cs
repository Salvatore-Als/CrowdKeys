using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class FlipVerticalEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        canvas.Save();
        canvas.Translate(dest.MidX, dest.MidY);
        canvas.Scale(1f, -1f);
        canvas.Translate(-dest.MidX, -dest.MidY);
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest);
        canvas.Restore();
    }
}
