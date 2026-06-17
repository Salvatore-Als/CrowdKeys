using SkiaSharp;

namespace CrowdKeys.ScreenEffects.Effects;

public class PixelateEffect : IScreenEffect
{
    private readonly int _blockSize;

    public PixelateEffect(int blockSize = 16) => _blockSize = blockSize;

    public void Apply(SKCanvas canvas, SKBitmap frame, double elapsedSec, SKRect dest)
    {
        int smallW = Math.Max(1, (int)(dest.Width  / _blockSize));
        int smallH = Math.Max(1, (int)(dest.Height / _blockSize));

        using var smallBmp    = new SKBitmap(smallW, smallH);
        using var smallCanvas = new SKCanvas(smallBmp);
        smallCanvas.DrawBitmap(frame, new SKRect(0, 0, frame.Width, frame.Height),
            new SKRect(0, 0, smallW, smallH));
        smallCanvas.Flush();

        using var image = SKImage.FromBitmap(smallBmp);
        canvas.DrawImage(image, new SKRect(0, 0, smallW, smallH), dest,
            new SKSamplingOptions(SKFilterMode.Nearest));
    }
}
