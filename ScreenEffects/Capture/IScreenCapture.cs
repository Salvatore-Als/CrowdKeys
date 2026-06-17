using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public interface IScreenCapture : IDisposable
{
    bool IsSupported { get; }
    SKBitmap? Capture();
}
