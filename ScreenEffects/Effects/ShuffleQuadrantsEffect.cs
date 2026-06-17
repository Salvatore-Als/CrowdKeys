using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ShuffleQuadrantsEffect : IScreenEffect
{
    private readonly int   _rows;
    private readonly int   _cols;
    private readonly int[] _order; // _order[dstIdx] = srcIdx

    // x2 : 1 row × 2 cols, swap left ↔ right
    public static ShuffleQuadrantsEffect X2() => new(1, 2, [1, 0]);

    // x4 : 2×2 grid, diagonal swap — TL↔BR, TR↔BL
    public static ShuffleQuadrantsEffect X4() => new(2, 2, [3, 2, 1, 0]);

    private ShuffleQuadrantsEffect(int rows, int cols, int[] order)
    {
        _rows  = rows;
        _cols  = cols;
        _order = order;
    }

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        float dw = dest.Width  / _cols;
        float dh = dest.Height / _rows;
        float fw = frame.Width  / (float)_cols;
        float fh = frame.Height / (float)_rows;

        for (int i = 0; i < _rows * _cols; i++)
        {
            int srcIdx = _order[i];
            int sc = srcIdx % _cols, sr = srcIdx / _cols;
            int dc = i      % _cols, dr = i      / _cols;

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
