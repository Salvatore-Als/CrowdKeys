using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace CrowdKeys.Views;

public partial class MonitorIndicatorWindow : Window
{
    [DllImport("user32.dll")] static extern int    GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int    SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool   SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    private const int  GWL_EXSTYLE            = -20;
    private const int  GWLP_WNDPROC           = -4;
    private const int  WS_EX_NOACTIVATE       = 0x08000000;
    private const int  WM_NCHITTEST           = 0x0084;
    private const int  HTTRANSPARENT          = -1;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private WndProcDelegate? _wndProc;
    private IntPtr _oldWndProc;

    public MonitorIndicatorWindow()
    {
        InitializeComponent();
        if (OperatingSystem.IsWindows())
            Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is not { } handle)
            return;

        var hwnd    = handle.Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

        _wndProc    = WndProcCallback;
        _oldWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProc));
    }

    private IntPtr WndProcCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCHITTEST)
            return new IntPtr(HTTRANSPARENT);
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    public void PositionOnScreen(int x, int y, int width, int height, double scaling)
    {
        var bannerX = x + (width - 400) / 2;
        Position = new PixelPoint(bannerX, y + 20);
    }

    public void SetLabel(string text)
    {
        LabelText.Text = text;
    }
}
