using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ShuffleQuadrantsEffect : IScreenEffect
{
    private readonly int[] _order;

    public ShuffleQuadrantsEffect()
    {
        _order = [0, 1, 2, 3];
        var rng = new Random();
        do
        {
            for (int i = _order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }
        while (_order[0] == 0 && _order[1] == 1 && _order[2] == 2 && _order[3] == 3);
    }

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        float dw = dest.Width / 2f, dh = dest.Height / 2f;
        float fw = frame.Width / 2f, fh = frame.Height / 2f;

        SKRectI[] src =
        [
            new(0,         0,          (int)fw, (int)fh),
            new((int)fw,   0,          frame.Width, (int)fh),
            new(0,         (int)fh,    (int)fw, frame.Height),
            new((int)fw,   (int)fh,    frame.Width, frame.Height),
        ];

        SKRect[] dst =
        [
            new(dest.Left,      dest.Top,      dest.Left + dw, dest.Top + dh),
            new(dest.Left + dw, dest.Top,      dest.Right,     dest.Top + dh),
            new(dest.Left,      dest.Top + dh, dest.Left + dw, dest.Bottom),
            new(dest.Left + dw, dest.Top + dh, dest.Right,     dest.Bottom),
        ];

        for (int i = 0; i < 4; i++)
            canvas.DrawBitmap(frame, src[_order[i]], dst[i]);
    }
}
