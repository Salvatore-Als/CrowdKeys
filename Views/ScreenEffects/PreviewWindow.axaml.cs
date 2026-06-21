using Avalonia.Controls;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame) =>
        EffectView.StartEffect(effect, frame);

    public void StartEffectLive(IScreenEffect effect, Func<SKBitmap?> frameProvider) =>
        EffectView.StartEffectLive(effect, frameProvider);

    public void StopEffect() =>
        EffectView.StopEffect();
}
