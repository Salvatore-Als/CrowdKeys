using Avalonia.Threading;
using CrowdKeys.Models;
using CrowdKeys.Views;

namespace CrowdKeys.Services;

public class ImageOverlayService : IDisposable
{
    private ImageOverlayWindow? _window;

    private bool   _monitorSet;
    private int    _monX, _monY, _monW, _monH;
    private double _monScaling = 1.0;

    private readonly Queue<(string path, ImagePosition position, int durationMs)> _queue = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _processing;

    public void SetMonitor(int x, int y, int width, int height, double scaling)
    {
        _monitorSet = true;
        _monX       = x;
        _monY       = y;
        _monW       = width;
        _monH       = height;
        _monScaling = scaling;

        Dispatcher.UIThread.Post(() =>
        {
            if (_window is { IsVisible: true })
                _window.PositionOnScreen(x, y, width, height, scaling);
        });
    }

    public void Enqueue(string filePath, ImagePosition position, int durationMs)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        lock (_queue)
            _queue.Enqueue((filePath, position, Math.Clamp(durationMs, 100, 30_000)));

        _ = EnsureProcessingAsync();
    }

    private Task EnsureProcessingAsync()
    {
        lock (_queue)
        {
            if (_processing)
                return Task.CompletedTask;

            _processing = true;
        }

        return Task.Run(ProcessQueueAsync);
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            (string path, ImagePosition position, int durationMs) item;
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
                await ShowOneAsync(item.path, item.position, item.durationMs, _disposeCts.Token);
            }
            catch (OperationCanceledException)
            {
                _processing = false;
                return;
            }
            catch { /* swallow per-item errors, continue queue */ }
        }
    }

    private async Task ShowOneAsync(string filePath, ImagePosition position, int durationMs, CancellationToken ct)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _window ??= new ImageOverlayWindow();
            if (_monitorSet)
                _window.PositionOnScreen(_monX, _monY, _monW, _monH, _monScaling);
            _window.ShowImage(filePath, position);
        });

        try
        {
            await Task.Delay(durationMs, ct);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => _window?.Hide());
        }
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        lock (_queue) _queue.Clear();
        Dispatcher.UIThread.Post(() => _window?.Close());
    }
}
