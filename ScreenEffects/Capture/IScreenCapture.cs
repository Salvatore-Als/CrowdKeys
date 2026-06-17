using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public interface IScreenCapture : IDisposable
{
    bool IsSupported { get; }
    SKBitmap? Capture();
    void CaptureInto(SKBitmap target);
    (int width, int height) ScreenSize { get; }
}
