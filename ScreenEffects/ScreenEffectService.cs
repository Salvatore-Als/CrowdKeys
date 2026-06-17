using Avalonia.Threading;
using CrowdKeys.Models;
using CrowdKeys.ScreenEffects.Effects;
using CrowdKeys.Views;
using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public class ScreenEffectService : IDisposable
{
    private readonly IScreenCapture _capture;
    private EffectOverlayWindow?    _overlay;

    private readonly SemaphoreSlim _queueLock = new(1, 1);
    private readonly Queue<(ScreenEffectType type, int durationMs)> _queue = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _processing;

    private readonly IScreenCapture _staticCapture; // GDI — for frozen-frame effects

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

    public void Enqueue(ScreenEffectType effectType, int durationMs)
    {
        if (!_capture.IsSupported)
            return;

        lock (_queue)
            _queue.Enqueue((effectType, durationMs));

        _ = EnsureProcessingAsync();
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

            try
            {
                await PlayOneAsync(item.type, item.durationMs, _disposeCts.Token);
            }
            catch (OperationCanceledException) { return; }
            catch { /* swallow per-item errors */ }
        }
    }

    private const int MaxEffectDurationMs = 10_000;
    private const int CaptureIntervalMs   = 33; // ~30 fps live capture

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
            ScreenEffectType.Blur               => new BlurEffect(),
            ScreenEffectType.Drunk              => new DrunkEffect(),
            _                                   => new MirrorEffect()
        };

        await PlayLiveAsync(effect, durationMs, ct);
    }

    // Static: capture once (GDI), render that frozen frame for the duration.
    private async Task PlayStaticAsync(IScreenEffect effect, int durationMs, CancellationToken ct)
    {
        var frame = _staticCapture.Capture();
        if (frame is null)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _overlay ??= new EffectOverlayWindow();
            _overlay.StartEffect(effect, frame);
            _overlay.Show();
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
            }, DispatcherPriority.Render);

            await Task.Delay(100, CancellationToken.None);
            frame.Dispose();
        }
    }

    // Live: double-buffer capture loop at ~30fps, effect renders the latest frame.
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
                catch (OperationCanceledException) { return; }
            }
        }, captureCts.Token);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _overlay ??= new EffectOverlayWindow();
            _overlay.StartEffectLive(effect, () => buffers[Volatile.Read(ref readIdx)]);
            _overlay.Show();
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
            }, DispatcherPriority.Render);

            await Task.Delay(100, CancellationToken.None);
            buffers[0].Dispose();
            buffers[1].Dispose();
        }
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        lock (_queue) _queue.Clear();
        _capture.Dispose();
        _staticCapture.Dispose();
        Dispatcher.UIThread.Post(() => _overlay?.Close());
    }
}
