using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrowdKeys.Views;

/// <summary>
/// Shows a red border around the target screen using 4 thin solid windows + a label.
/// Each strip is opaque (no transparency complications), HTTRANSPARENT WndProc makes them click-through.
/// </summary>
internal sealed class MonitorBorderOverlay
{
    [DllImport("user32.dll")] static extern int    GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int    SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool   SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    const int  GWL_EXSTYLE            = -20;
    const int  GWLP_WNDPROC           = -4;
    const int  WS_EX_NOACTIVATE       = 0x08000000;
    const int  WM_NCHITTEST           = 0x0084;
    const int  HTTRANSPARENT          = -1;
    const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate IntPtr WndProcFn(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static readonly SolidColorBrush RedBrush =
        new(Color.FromRgb(0xe5, 0x39, 0x35));

    private readonly List<Window> _windows = [];

    public void ShowOnScreen(int x, int y, int w, int h, double scaling, string label)
    {
        CloseAll();

        const int T = 6; // border thickness (physical pixels)

        AddStrip(x,         y,         w,       T,       scaling); // top
        AddStrip(x,         y+h-T,     w,       T,       scaling); // bottom
        AddStrip(x,         y+T,       T,       h-T*2,   scaling); // left
        AddStrip(x+w-T,     y+T,       T,       h-T*2,   scaling); // right

        var labelWin = new MonitorIndicatorWindow();
        labelWin.SetLabel(label);
        labelWin.Position = new PixelPoint(x + w - (int)(240 * scaling), y + T + 4);
        _windows.Add(labelWin);

        foreach (var win in _windows)
            win.Show();
    }

    public void CloseAll()
    {
        foreach (var win in _windows)
            win.Close();
        _windows.Clear();
    }

    private void AddStrip(int px, int py, int pw, int ph, double scaling)
    {
        if (pw <= 0 || ph <= 0) return;

        var win = new Window
        {
            WindowDecorations    = WindowDecorations.None,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Topmost              = true,
            ShowInTaskbar        = false,
            ShowActivated        = false,
            CanResize            = false,
            Focusable            = false,
            IsHitTestVisible     = false,
            Background           = RedBrush,
            Position             = new PixelPoint(px, py),
            Width                = pw / scaling,
            Height               = ph / scaling,
        };

        if (OperatingSystem.IsWindows())
        {
            win.Opened += (_, _) =>
            {
                if (win.TryGetPlatformHandle() is not { } h) return;
                var hwnd = h.Handle;

                SetWindowLong(hwnd, GWL_EXSTYLE,
                    GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

                IntPtr oldProc = IntPtr.Zero;
                WndProcFn proc = (hWnd2, msg, wParam, lParam) =>
                {
                    if (msg == WM_NCHITTEST) return new IntPtr(HTTRANSPARENT);
                    return CallWindowProc(oldProc, hWnd2, msg, wParam, lParam);
                };
                win.Tag = proc; // prevent GC collection
                oldProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(proc));
            };
        }

        _windows.Add(win);
    }
}
