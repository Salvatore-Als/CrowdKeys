using Avalonia;
using Avalonia.Controls;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
        // Off-screen so OBS can still Window Capture it without it floating on the streamer's desktop
        Position = new PixelPoint(-32000, -32000);
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame) =>
        EffectView.StartEffect(effect, frame);

    public void StartEffectLive(IScreenEffect effect, Func<SKBitmap?> frameProvider) =>
        EffectView.StartEffectLive(effect, frameProvider);

    public void StopEffect() =>
        EffectView.StopEffect();
}
