using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class ChromaticAberrationEffect : IScreenEffect
{
    private static readonly float[] RedMatrix   = [1,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0, 0,0,0,1,0];
    private static readonly float[] GreenMatrix = [0,0,0,0,0, 0,1,0,0,0, 0,0,0,0,0, 0,0,0,1,0];
    private static readonly float[] BlueMatrix  = [0,0,0,0,0, 0,0,0,0,0, 0,0,1,0,0, 0,0,0,1,0];

    private readonly float _offset;

    public ChromaticAberrationEffect(float offset = 10f) => _offset = offset;

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        var src = new SKRect(0, 0, frame.Width, frame.Height);

        canvas.Clear(SKColors.Black);

        using var redPaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(RedMatrix),
            BlendMode   = SKBlendMode.Plus,
        };
        using var greenPaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(GreenMatrix),
            BlendMode   = SKBlendMode.Plus,
        };
        using var bluePaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(BlueMatrix),
            BlendMode   = SKBlendMode.Plus,
        };

        canvas.DrawBitmap(frame, src,
            new SKRect(dest.Left - _offset, dest.Top, dest.Right - _offset, dest.Bottom), redPaint);
        canvas.DrawBitmap(frame, src, dest, greenPaint);
        canvas.DrawBitmap(frame, src,
            new SKRect(dest.Left + _offset, dest.Top, dest.Right + _offset, dest.Bottom), bluePaint);
    }
}
