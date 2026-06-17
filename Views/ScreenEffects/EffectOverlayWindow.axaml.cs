using Avalonia.Controls;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public partial class EffectOverlayWindow : Window
{
    public EffectOverlayWindow()
    {
        InitializeComponent();
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame) =>
        EffectView.StartEffect(effect, frame);

    public void StopEffect() =>
        EffectView.StopEffect();
}
