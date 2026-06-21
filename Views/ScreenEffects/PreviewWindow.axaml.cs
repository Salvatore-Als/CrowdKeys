using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public partial class PreviewWindow : Window
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080; // hide from Alt+Tab

    public PreviewWindow()
    {
        InitializeComponent();
        Position = new PixelPoint(-32000, -32000);

        if (OperatingSystem.IsWindows())
            Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is not { } handle)
            return;
        var exStyle = GetWindowLong(handle.Handle, GWL_EXSTYLE);
        SetWindowLong(handle.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame) =>
        EffectView.StartEffect(effect, frame);

    public void StartEffectLive(IScreenEffect effect, Func<SKBitmap?> frameProvider) =>
        EffectView.StartEffectLive(effect, frameProvider);

    public void StopEffect() =>
        EffectView.StopEffect();
}
