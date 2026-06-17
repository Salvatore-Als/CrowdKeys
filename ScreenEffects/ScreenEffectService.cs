using Avalonia.Threading;
using CrowdKeys.Models;
using CrowdKeys.ScreenEffects.Effects;
using CrowdKeys.Views;

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

    private async Task PlayOneAsync(ScreenEffectType effectType, int durationMs, CancellationToken ct)
    {
        await Dispatcher.UIThread.InvokeAsync(() => _overlay?.Hide(), DispatcherPriority.Render);
        await Task.Delay(80, ct);

        var frame = _capture.Capture();
        if (frame is null)
            return;

        IScreenEffect effect = effectType switch
        {
            ScreenEffectType.Mirror           => new MirrorEffect(),
            ScreenEffectType.ShuffleQuadrants => new ShuffleQuadrantsEffect(),
            ScreenEffectType.Blur             => new BlurEffect(),
            ScreenEffectType.Drunk            => new DrunkEffect(),
            _                                 => new MirrorEffect()
        };

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
            frame.Dispose();
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
