using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class MirrorEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        canvas.Save();
        canvas.Translate(dest.Right, dest.Top);
        canvas.Scale(-1f, 1f);
        canvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height), dest with { Left = 0, Right = dest.Width });
        canvas.Restore();
    }
}
