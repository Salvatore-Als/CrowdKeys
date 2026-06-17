using System.Runtime.InteropServices;
using Avalonia.Controls;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public partial class EffectOverlayWindow : Window
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_LAYERED    = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    public EffectOverlayWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsWindows())
            Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is { } handle)
        {
            var exStyle = GetWindowLong(handle.Handle, GWL_EXSTYLE);
            SetWindowLong(handle.Handle, GWL_EXSTYLE,
                exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
        }
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame) =>
        EffectView.StartEffect(effect, frame);

    public void StopEffect() =>
        EffectView.StopEffect();
}
