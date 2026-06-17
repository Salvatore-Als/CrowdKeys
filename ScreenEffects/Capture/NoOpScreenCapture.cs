using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public class NoOpScreenCapture : IScreenCapture
{
    public bool IsSupported => false;
    public SKBitmap? Capture() => null;
    public void Dispose() { }
}
