using System.Runtime.InteropServices;
using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

public class WindowsGdiCapture : IScreenCapture
{
    public bool IsSupported => true;
    public (int width, int height) ScreenSize =>
        (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));

    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern int    ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]  static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    [DllImport("gdi32.dll")]  static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int w, int h);
    [DllImport("gdi32.dll")]  static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);
    [DllImport("gdi32.dll")]  static extern bool   BitBlt(IntPtr hDst, int x, int y, int w, int h, IntPtr hSrc, int xs, int ys, uint rop);
    [DllImport("gdi32.dll")]  static extern bool   DeleteDC(IntPtr hDC);
    [DllImport("gdi32.dll")]  static extern bool   DeleteObject(IntPtr hObj);
    [DllImport("gdi32.dll")]  static extern int    GetDIBits(IntPtr hDC, IntPtr hBmp, int start, int lines, byte[] bits, ref BITMAPINFO bmi, uint usage);
    [DllImport("user32.dll")] static extern int    GetSystemMetrics(int idx);

    const uint SRCCOPY        = 0xCC0020;
    const uint DIB_RGB_COLORS = 0;
    const int  SM_CXSCREEN    = 0;
    const int  SM_CYSCREEN    = 1;

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public int   biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public uint  biCompression, biSizeImage;
        public int   biXPels, biYPels;
        public uint  biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public uint[] bmiColors;
    }

    // Reusable pixel buffer — avoids per-frame allocation in CaptureInto.
    private byte[]? _pixelBuf;

    public SKBitmap? Capture()
    {
        int w = GetSystemMetrics(SM_CXSCREEN);
        int h = GetSystemMetrics(SM_CYSCREEN);

        var bmp = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
        CaptureInto(bmp);
        return bmp;
    }

    public void CaptureInto(SKBitmap target)
    {
        int w = target.Width;
        int h = target.Height;

        var desktop = GetDesktopWindow();
        var srcDC   = GetDC(desktop);
        var memDC   = CreateCompatibleDC(srcDC);
        var hBmp    = CreateCompatibleBitmap(srcDC, w, h);
        var oldBmp  = SelectObject(memDC, hBmp);

        try
        {
            BitBlt(memDC, 0, 0, w, h, srcDC, 0, 0, SRCCOPY);

            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize     = Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth    = w,
                    biHeight   = -h,
                    biPlanes   = 1,
                    biBitCount = 32,
                },
                bmiColors = new uint[1]
            };

            int needed = w * h * 4;
            if (_pixelBuf is null || _pixelBuf.Length < needed)
                _pixelBuf = new byte[needed];

            GetDIBits(memDC, hBmp, 0, h, _pixelBuf, ref bmi, DIB_RGB_COLORS);
            Marshal.Copy(_pixelBuf, 0, target.GetPixels(), needed);
        }
        finally
        {
            SelectObject(memDC, oldBmp);
            DeleteObject(hBmp);
            DeleteDC(memDC);
            ReleaseDC(desktop, srcDC);
        }
    }

    public void Dispose() { }
}
