using System.Runtime.InteropServices;
using Avalonia.Controls;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public partial class EffectOverlayWindow : Window
{
    [DllImport("user32.dll")] private static extern int  GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int  SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    private const int  GWL_EXSTYLE             = -20;
    private const int  WS_EX_LAYERED           = 0x00080000;
    private const int  WS_EX_TRANSPARENT       = 0x00000020;
    private const int  WS_EX_NOACTIVATE        = 0x08000000;
    private const uint WDA_EXCLUDEFROMCAPTURE  = 0x00000011;

    public EffectOverlayWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsWindows())
            Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is not { } handle)
            return;

        var hwnd = handle.Handle;

        // Click-through + no focus steal
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE,
            exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);

        // Exclude this window from GDI/DX screen captures so the live
        // capture loop doesn't capture the overlay itself (feedback loop).
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame) =>
        EffectView.StartEffect(effect, frame);

    public void StartEffectLive(IScreenEffect effect, Func<SKBitmap?> frameProvider) =>
        EffectView.StartEffectLive(effect, frameProvider);

    public void StopEffect() =>
        EffectView.StopEffect();
}
