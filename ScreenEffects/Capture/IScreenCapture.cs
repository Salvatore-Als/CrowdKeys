using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public interface IScreenCapture : IDisposable
{
    bool IsSupported { get; }
    SKBitmap? Capture();
    void CaptureInto(SKBitmap target);
    (int width, int height) ScreenSize { get; }
    void SetMonitor(int monitorIndex, int x, int y, int width, int height);
}
