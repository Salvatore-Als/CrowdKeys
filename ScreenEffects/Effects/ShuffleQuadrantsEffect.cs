using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ShuffleQuadrantsEffect : IScreenEffect
{
    private readonly int   _divisions;
    private readonly int[] _order;

    public ShuffleQuadrantsEffect(int divisions = 2)
    {
        _divisions = divisions;
        int count  = divisions * divisions;
        _order     = Enumerable.Range(0, count).ToArray();

        var rng = new Random();
        do
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }
        while (Enumerable.Range(0, count).All(i => _order[i] == i));
    }

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        int   n  = _divisions;
        float dw = dest.Width  / n;
        float dh = dest.Height / n;
        float fw = frame.Width  / (float)n;
        float fh = frame.Height / (float)n;

        for (int i = 0; i < n * n; i++)
        {
            int srcIdx = _order[i];
            int sc = srcIdx % n, sr = srcIdx / n;
            int dc = i      % n, dr = i      / n;

            var src = new SKRectI(
                (int)(sc * fw),       (int)(sr * fh),
                (int)((sc + 1) * fw), (int)((sr + 1) * fh));
            
            var dst = new SKRect(
                dest.Left + dc * dw,       dest.Top + dr * dh,
                dest.Left + (dc + 1) * dw, dest.Top + (dr + 1) * dh);

            canvas.DrawBitmap(frame, src, dst);
        }
    }
}
