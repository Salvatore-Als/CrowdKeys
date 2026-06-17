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

    public ScreenEffectService()
    {
        _capture = OperatingSystem.IsWindows()
            ? new WindowsGdiCapture()
            : new NoOpScreenCapture();
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

        var (w, h) = _capture.ScreenSize;
        if (w == 0 || h == 0)
            return;

        // Double-buffer: capture loop writes to one bitmap, render reads from the other.
        var buffers = new[]
        {
            new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque),
            new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque),
        };

        // Fill both buffers before starting so the first render has valid data.
        _capture.CaptureInto(buffers[0]);
        _capture.CaptureInto(buffers[1]);

        int readIdx  = 0; // index of the buffer safe to read from
        int writeIdx = 1;

        IScreenEffect effect = effectType switch
        {
            ScreenEffectType.Mirror             => new MirrorEffect(),
            ScreenEffectType.ShuffleQuadrants   => new ShuffleQuadrantsEffect(2),
            ScreenEffectType.ShuffleQuadrants4  => new ShuffleQuadrantsEffect(4),
            ScreenEffectType.ShuffleQuadrants8  => new ShuffleQuadrantsEffect(8),
            ScreenEffectType.ShuffleQuadrants16 => new ShuffleQuadrantsEffect(16),
            ScreenEffectType.ShuffleQuadrants32 => new ShuffleQuadrantsEffect(32),
            ScreenEffectType.ShuffleQuadrants64 => new ShuffleQuadrantsEffect(64),
            ScreenEffectType.Blur               => new BlurEffect(),
            ScreenEffectType.Drunk              => new DrunkEffect(),
            _                                   => new MirrorEffect()
        };

        using var captureCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Background capture loop — writes to writeIdx, then atomically promotes to readIdx.
        _ = Task.Run(async () =>
        {
            while (!captureCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CaptureIntervalMs, captureCts.Token);
                    _capture.CaptureInto(buffers[writeIdx]);
                    Volatile.Write(ref readIdx, writeIdx);
                    writeIdx ^= 1; // flip: old read buffer becomes next write target
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

            // Wait for any in-flight render ops to drain before freeing the bitmaps.
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
        Dispatcher.UIThread.Post(() => _overlay?.Close());
    }
}
