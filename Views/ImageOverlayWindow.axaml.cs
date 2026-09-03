using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using CrowdKeys.Models;

namespace CrowdKeys.Views;

public partial class ImageOverlayWindow : Window
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE       = -20;
    private const int WS_EX_LAYERED     = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE  = 0x08000000;

    private string? _loadedPath;

    public ImageOverlayWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsWindows())
            Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is not { } handle)
            return;

        var hwnd = handle.Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
    }

    public void PositionOnScreen(int x, int y, int width, int height, double scaling)
    {
        WindowState = WindowState.Normal;
        Position    = new PixelPoint(x, y);
        Width       = width  / scaling;
        Height      = height / scaling;
        WindowState = WindowState.FullScreen;
    }

    public void ShowImage(string filePath, ImagePosition position)
    {
        if (_loadedPath != filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                ImageView.Source = new Bitmap(stream);
                _loadedPath = filePath;
            }
            catch
            {
                return;
            }
        }

        var (h, v) = position switch
        {
            ImagePosition.TopLeft      => (HorizontalAlignment.Left,   VerticalAlignment.Top),
            ImagePosition.TopCenter    => (HorizontalAlignment.Center, VerticalAlignment.Top),
            ImagePosition.TopRight     => (HorizontalAlignment.Right,  VerticalAlignment.Top),
            ImagePosition.MiddleLeft   => (HorizontalAlignment.Left,   VerticalAlignment.Center),
            ImagePosition.MiddleRight  => (HorizontalAlignment.Right,  VerticalAlignment.Center),
            ImagePosition.BottomLeft   => (HorizontalAlignment.Left,   VerticalAlignment.Bottom),
            ImagePosition.BottomCenter => (HorizontalAlignment.Center, VerticalAlignment.Bottom),
            ImagePosition.BottomRight  => (HorizontalAlignment.Right,  VerticalAlignment.Bottom),
            _                          => (HorizontalAlignment.Center, VerticalAlignment.Center),
        };

        ImageView.HorizontalAlignment = h;
        ImageView.VerticalAlignment   = v;

        Show();
    }
}
