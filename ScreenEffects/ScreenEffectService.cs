using Avalonia;
using Avalonia.Threading;
using CrowdKeys.Models;
using CrowdKeys.ScreenEffects.Effects;
using CrowdKeys.Views;
using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public class ScreenEffectService : IDisposable
{
    private readonly IScreenCapture _capture;
    private readonly IScreenCapture _staticCapture;

    public event Action? PreviewClosed;
    public event Action? EffectBypassed;

    private EffectOverlayWindow?  _overlay;
    private PreviewWindow?        _previewWindow;
    private MonitorBorderOverlay  _borderOverlay = new();

    // monitor state (physical pixels + scaling for logical sizing)
    private bool   _monitorSet = false;
    private int    _monX, _monY, _monW, _monH;
    private double _monScaling = 1.0;

    private readonly SemaphoreSlim _queueLock = new(1, 1);
    private readonly Queue<(ScreenEffectType type, int durationMs)> _queue = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private volatile CancellationTokenSource _effectCts  = new();
    private bool _processing;

    public ScreenEffectService()
    {
        if (OperatingSystem.IsWindows())
        {
            var dxgi = new WindowsDxgiCapture();
            _capture       = dxgi.IsSupported ? dxgi : new WindowsGdiCapture();
            _staticCapture = new WindowsGdiCapture();
        }
        else
        {
            _capture       = new NoOpScreenCapture();
            _staticCapture = new NoOpScreenCapture();
        }
    }

    private string _monitorLabel = "";

    public void SetMonitor(int index, int x, int y, int width, int height, double scaling, string label = "")
    {
        _monitorSet   = true;
        _monX         = x;
        _monY         = y;
        _monW         = width;
        _monH         = height;
        _monScaling   = scaling;
        _monitorLabel = label;

        _capture.SetMonitor(index, x, y, width, height);
        _staticCapture.SetMonitor(index, x, y, width, height);

        Dispatcher.UIThread.Post(() =>
        {
            if (_overlay is { IsVisible: true })
                _overlay.PositionOnScreen(x, y, width, height, scaling);

            ShowIndicator(x, y, width, height, scaling, label);
        });
    }

    private void ShowIndicator(int x, int y, int width, int height, double scaling, string label)
    {
        _borderOverlay.ShowOnScreen(x, y, width, height, scaling, label);
        Task.Delay(2500).ContinueWith(_ =>
            Dispatcher.UIThread.Post(() => _borderOverlay.CloseAll()));
    }

    public void OpenPreviewWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_previewWindow is null)
            {
                _previewWindow = new PreviewWindow();
                _previewWindow.Closed += (_, _) =>
                {
                    _previewWindow = null;
                    PreviewClosed?.Invoke();
                };
                _previewWindow.Show();
            }
        });
    }

    public void Enqueue(ScreenEffectType effectType, int durationMs)
    {
        if (!_capture.IsSupported)
            return;

        if (_previewWindow is null)
        {
            EffectBypassed?.Invoke();
            return;
        }

        lock (_queue)
            _queue.Enqueue((effectType, durationMs));

        _ = EnsureProcessingAsync();
    }

    public void StopAll()
    {
        lock (_queue)
            _queue.Clear();

        var old = Interlocked.Exchange(ref _effectCts, new CancellationTokenSource());
        old.Cancel();
    }

    private async Task EnsureProcessingAsync()
    {
        await _queueLock.WaitAsync();
        if (_processing)
        {
            _queueLock.Release();
            return;
        }
        _processing = true;
        _queueLock.Release();

        _ = Task.Run(ProcessQueueAsync);
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            (ScreenEffectType type, int durationMs) item;
            lock (_queue)
            {
                if (_queue.Count == 0)
                {
                    _processing = false;
                    return;
                }
                item = _queue.Dequeue();
            }

            var effectCts = _effectCts; // snapshot before await
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                effectCts.Token, _disposeCts.Token);

            try
            {
                await PlayOneAsync(item.type, item.durationMs, linked.Token);
            }
            catch (OperationCanceledException)
            {
                _processing = false; // fix: was never reset on early exit
                return;
            }
            catch { /* swallow per-item errors, continue queue */ }
        }
    }

    private const int MaxEffectDurationMs = 10_000;
    private const int CaptureIntervalMs   = 16; // ~60 fps live capture

    private async Task PlayOneAsync(ScreenEffectType effectType, int durationMs, CancellationToken ct)
    {
        durationMs = Math.Min(durationMs, MaxEffectDurationMs);

        await Dispatcher.UIThread.InvokeAsync(() => _overlay?.Hide(), DispatcherPriority.Render);
        await Task.Delay(80, ct);

        IScreenEffect effect = effectType switch
        {
            ScreenEffectType.Mirror             => new MirrorEffect(),
            ScreenEffectType.ShuffleQuadrants  => ShuffleQuadrantsEffect.X2(),
            ScreenEffectType.ShuffleQuadrants4 => ShuffleQuadrantsEffect.X4(),
            ScreenEffectType.Blur                => new BlurEffect(),
            ScreenEffectType.Drunk               => new DrunkEffect(),
            ScreenEffectType.FlipVertical        => new FlipVerticalEffect(),
            ScreenEffectType.InvertColors        => new InvertColorsEffect(),
            ScreenEffectType.Grayscale           => new GrayscaleEffect(),
            ScreenEffectType.Pixelate            => new PixelateEffect(),
            ScreenEffectType.ZoomIn              => new ZoomInEffect(),
            ScreenEffectType.ChromaticAberration => new ChromaticAberrationEffect(),
            ScreenEffectType.Glitch              => new GlitchEffect(),
            ScreenEffectType.Scanlines           => new ScanlinesEffect(),
            ScreenEffectType.ZoomPulse           => new ZoomPulseEffect(),
            _                                   => new MirrorEffect()
        };

        await PlayLiveAsync(effect, durationMs, ct);
    }

    private async Task PlayStaticAsync(IScreenEffect effect, int durationMs, CancellationToken ct)
    {
        var frame = _staticCapture.Capture();
        if (frame is null)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _overlay ??= new EffectOverlayWindow();
            if (_monitorSet)
                _overlay.PositionOnScreen(_monX, _monY, _monW, _monH, _monScaling);
            _overlay.StartEffect(effect, frame);
            _overlay.Show();

            _previewWindow ??= new PreviewWindow();
            _previewWindow.StartEffect(effect, frame);
            _previewWindow.Show();
        }, DispatcherPriority.Render);

        try
        {
            await Task.Delay(durationMs, ct);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _overlay?.StopEffect();
                _overlay?.Hide();
                _previewWindow?.StopEffect();
            }, DispatcherPriority.Render);

            GC.KeepAlive(frame);
        }
    }

    private async Task PlayLiveAsync(IScreenEffect effect, int durationMs, CancellationToken ct)
    {
        var (w, h) = _capture.ScreenSize;
        if (w == 0 || h == 0)
            return;

        var buffers = new[]
        {
            new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque),
            new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque),
        };

        _capture.CaptureInto(buffers[0]);
        _capture.CaptureInto(buffers[1]);

        int readIdx  = 0;
        int writeIdx = 1;

        using var captureCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _ = Task.Run(async () =>
        {
            while (!captureCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CaptureIntervalMs, captureCts.Token);
                    _capture.CaptureInto(buffers[writeIdx]);
                    Volatile.Write(ref readIdx, writeIdx);
                    writeIdx ^= 1;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }, captureCts.Token);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _overlay ??= new EffectOverlayWindow();
            if (_monitorSet)
                _overlay.PositionOnScreen(_monX, _monY, _monW, _monH, _monScaling);
            _overlay.StartEffectLive(effect, () => buffers[Volatile.Read(ref readIdx)]);
            _overlay.Show();

            _previewWindow ??= new PreviewWindow();
            _previewWindow.StartEffectLive(effect, () => buffers[Volatile.Read(ref readIdx)]);
            _previewWindow.Show();
        }, DispatcherPriority.Render);

        try
        {
            await Task.Delay(durationMs, ct);
        }
        finally
        {
            captureCts.Cancel();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _overlay?.StopEffect();
                _overlay?.Hide();
                _previewWindow?.StopEffect();
            }, DispatcherPriority.Render);

            GC.KeepAlive(buffers[0]);
            GC.KeepAlive(buffers[1]);
        }
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        lock (_queue) _queue.Clear();
        _capture.Dispose();
        _staticCapture.Dispose();
        Dispatcher.UIThread.Post(() =>
        {
            _overlay?.Close();
            _previewWindow?.Close();
            _borderOverlay.CloseAll();
        });
    }
}
