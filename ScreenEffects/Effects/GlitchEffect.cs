using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class GlitchEffect : IScreenEffect
{
    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        // New glitch pattern every ~80ms
        var rng  = new Random((int)(elapsedSec * 12.5));
        float fh = frame.Height;
        float fw = frame.Width;
        float scaleY = dest.Height / fh;
        float scaleX = dest.Width  / fw;

        float y = 0;
        while (y < fh)
        {
            float bandH = rng.Next(3, 50);
            float shift = rng.NextSingle() < 0.3f
                ? (rng.NextSingle() - 0.5f) * 120f * scaleX
                : 0f;

            float y2 = Math.Min(y + bandH, fh);

            var src = new SKRect(0, y, fw, y2);
            var dst = new SKRect(
                dest.Left + shift,  dest.Top + y  * scaleY,
                dest.Right + shift, dest.Top + y2 * scaleY);

            canvas.DrawBitmap(frame, src, dst);
            y += bandH;
        }
    }
}
