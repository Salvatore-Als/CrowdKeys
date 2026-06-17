using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public class NoOpScreenCapture : IScreenCapture
{
    public bool IsSupported => false;
    public (int width, int height) ScreenSize => (0, 0);
    public SKBitmap? Capture() => null;
    public void CaptureInto(SKBitmap target) { }
    public void Dispose() { }
}
