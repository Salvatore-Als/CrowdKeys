using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using CrowdKeys.ScreenEffects.Effects;
using SkiaSharp;

namespace CrowdKeys.Views;

public class EffectCanvas : Avalonia.Controls.Control
{
    private IScreenEffect?    _effect;
    private SKBitmap?         _frame;
    private Func<SKBitmap?>?  _frameProvider;
    private readonly Stopwatch _sw = new();
    private DispatcherTimer?  _timer;

    public EffectCanvas()
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch;
    }

    public void StartEffect(IScreenEffect effect, SKBitmap frame)
    {
        _effect        = effect;
        _frame         = frame;
        _frameProvider = null;
        _sw.Restart();

        _timer?.Stop();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render,
            (_, _) => InvalidateVisual());
        _timer.Start();
    }

    public void StartEffectLive(IScreenEffect effect, Func<SKBitmap?> frameProvider)
    {
        _effect        = effect;
        _frame         = null;
        _frameProvider = frameProvider;
        _sw.Restart();

        _timer?.Stop();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render,
            (_, _) => InvalidateVisual());
        _timer.Start();
    }

    public void StopEffect()
    {
        _timer?.Stop();
        _timer         = null;
        _effect        = null;
        _frame         = null;
        _frameProvider = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var effect = _effect;
        var frame  = _frameProvider?.Invoke() ?? _frame;
        if (effect is null || frame is null)
            return;

        var elapsed = _sw.Elapsed.TotalSeconds;
        var bounds  = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.Custom(new EffectRenderOp(effect, frame, elapsed, bounds));
    }
}

file sealed class EffectRenderOp : ICustomDrawOperation
{
    private readonly IScreenEffect _effect;
    private readonly SKBitmap      _frame;
    private readonly double        _elapsed;
    private readonly Rect          _bounds;

    public EffectRenderOp(IScreenEffect effect, SKBitmap frame, double elapsed, Rect bounds)
    {
        _effect  = effect;
        _frame   = frame;
        _elapsed = elapsed;
        _bounds  = bounds;
    }

    public Rect Bounds => _bounds;
    public bool HitTest(Point p) => false;
    public bool Equals(ICustomDrawOperation? other) => false;
    public void Dispose() { }

    public void Render(ImmediateDrawingContext context)
    {
        var skia = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skia is null)
            return;

        using var lease = skia.Lease();
        var canvas = lease.SkCanvas;
        canvas.Clear(SKColors.Black);

        var dest = new SKRect(
            (float)_bounds.Left,
            (float)_bounds.Top,
            (float)_bounds.Right,
            (float)_bounds.Bottom);

        _effect.Apply(canvas, _frame, _elapsed, dest);
    }
}
