using System.Runtime.InteropServices;
using SkiaSharp;

namespace CrowdKeys.ScreenEffects;

/// <summary>
/// Screen capture via DXGI Desktop Duplication.
/// Captures the DWM-composited frame before our overlay is applied.
/// Combined with WDA_EXCLUDEFROMCAPTURE on the overlay, the capture
/// sees only the game — no feedback loop.
/// Requires Windows 8+ and a D3D11-capable GPU.
/// </summary>
public class WindowsDxgiCapture : IScreenCapture
{
    // ── P/Invoke ───────────────────────────────────────────────────────────

    [DllImport("d3d11.dll")]
    static extern int D3D11CreateDevice(
        IntPtr pAdapter, int DriverType, IntPtr Software, int Flags,
        IntPtr pFeatureLevels, int FeatureLevels, int SDKVersion,
        out IntPtr ppDevice, out int pFeatureLevel, out IntPtr ppContext);

    [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);

    // ── COM vtable helpers ─────────────────────────────────────────────────

    static T Vtable<T>(IntPtr obj, int slot) where T : Delegate
    {
        var vtbl = Marshal.ReadIntPtr(obj);
        var fn   = Marshal.ReadIntPtr(vtbl + slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(fn);
    }

    static IntPtr QI(IntPtr obj, Guid iid)
    {
        Vtable<QIFn>(obj, 0)(obj, ref iid, out var p);
        return p;
    }

    static void Release(ref IntPtr p)
    {
        if (p == IntPtr.Zero) return;
        Vtable<RelFn>(p, 2)(p);
        p = IntPtr.Zero;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int  QIFn(IntPtr t, ref Guid g, out IntPtr p);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint RelFn(IntPtr t);

    // ── DXGI delegates ─────────────────────────────────────────────────────

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int GetAdapterFn(IntPtr t, out IntPtr pp);          // IDXGIDevice slot 7

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int EnumOutputsFn(IntPtr t, int i, out IntPtr pp);  // IDXGIAdapter slot 7

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int DuplicateOutputFn(IntPtr t, IntPtr dev, out IntPtr pp); // IDXGIOutput1 slot 22

    [StructLayout(LayoutKind.Sequential)]
    struct FrameInfo
    {
        public long LastPresentTime, LastMouseUpdateTime;
        public int  AccumulatedFrames, RectsCoalesced, ProtectedContentMaskedOut;
        public int  PtrX, PtrY, PtrVisible;
        public int  TotalMetadataBufferSize, PointerShapeBufferSize;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int AcquireFrameFn(IntPtr t, int timeout, out FrameInfo fi, out IntPtr res); // IDXGIOutputDuplication slot 8

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int ReleaseFrameFn(IntPtr t);                       // IDXGIOutputDuplication slot 14

    // ── D3D11 delegates ────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    struct Tex2DDesc
    {
        public int W, H, Mips, Arrays, Format;
        public int SampleCount, SampleQuality;
        public int Usage, Bind, CpuAccess, Misc;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CreateTex2DFn(IntPtr t, ref Tex2DDesc d, IntPtr init, out IntPtr pp); // ID3D11Device slot 5

    [StructLayout(LayoutKind.Sequential)]
    struct Mapped { public IntPtr pData; public int RowPitch, DepthPitch; }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int  MapFn(IntPtr t, IntPtr res, int sub, int type, int flags, out Mapped m); // slot 14
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void UnmapFn(IntPtr t, IntPtr res, int sub);                                  // slot 15
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void CopyFn(IntPtr t, IntPtr dst, IntPtr src);                                // slot 47

    // ── GUIDs ──────────────────────────────────────────────────────────────

    static readonly Guid IID_IDXGIDevice    = new("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
    static readonly Guid IID_IDXGIOutput1   = new("00CDDEA8-939B-4B83-A340-A685226666CC");
    static readonly Guid IID_ID3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    const int DXGI_ERROR_WAIT_TIMEOUT = unchecked((int)0x887A0027);
    const int DXGI_ERROR_ACCESS_LOST  = unchecked((int)0x887A0026);

    // ── State ──────────────────────────────────────────────────────────────

    IntPtr _device, _context, _duplication, _stagingTex;
    int    _width, _height;
    byte[] _rowBuf = Array.Empty<byte>();
    int    _monitorIndex = 0;

    AcquireFrameFn? _acquire;
    ReleaseFrameFn? _releaseFrame;
    MapFn?          _map;
    UnmapFn?        _unmap;
    CopyFn?         _copy;

    public bool IsSupported           { get; private set; }
    public (int width, int height) ScreenSize => (_width, _height);

    public WindowsDxgiCapture()
    {
        try   { Init(); }
        catch { IsSupported = false; }
    }

    void Init()
    {
        if (D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0, IntPtr.Zero, 0, 7,
            out _device, out _, out _context) < 0) return;

        InitOutput();
    }

    void InitOutput()
    {
        Release(ref _duplication);
        Release(ref _stagingTex);
        _acquire      = null;
        _releaseFrame = null;
        IsSupported   = false;

        if (_device == IntPtr.Zero) return;

        var dxgiDev = QI(_device, IID_IDXGIDevice);
        if (dxgiDev == IntPtr.Zero) return;

        Vtable<GetAdapterFn>(dxgiDev, 7)(dxgiDev, out var adapter);
        Release(ref dxgiDev);
        if (adapter == IntPtr.Zero) return;

        Vtable<EnumOutputsFn>(adapter, 7)(adapter, _monitorIndex, out var output);
        Release(ref adapter);
        if (output == IntPtr.Zero) return;

        var output1 = QI(output, IID_IDXGIOutput1);
        Release(ref output);
        if (output1 == IntPtr.Zero) return;

        int hr = Vtable<DuplicateOutputFn>(output1, 22)(output1, _device, out _duplication);
        Release(ref output1);
        if (hr < 0) return;

        if (_width == 0 || _height == 0)
        {
            _width  = GetSystemMetrics(0);
            _height = GetSystemMetrics(1);
        }
        _rowBuf = new byte[_width * 4];

        var desc = new Tex2DDesc
        {
            W = _width, H = _height, Mips = 1, Arrays = 1,
            Format      = 87,        // DXGI_FORMAT_B8G8R8A8_UNORM
            SampleCount = 1,
            Usage       = 3,         // D3D11_USAGE_STAGING
            CpuAccess   = 0x20000,   // D3D11_CPU_ACCESS_READ
        };

        if (Vtable<CreateTex2DFn>(_device, 5)(_device, ref desc, IntPtr.Zero, out _stagingTex) < 0)
            return;

        _acquire      = Vtable<AcquireFrameFn>(_duplication, 8);
        _releaseFrame = Vtable<ReleaseFrameFn>(_duplication, 14);
        _copy         = Vtable<CopyFn>(_context, 47);
        _map          = Vtable<MapFn>(_context, 14);
        _unmap        = Vtable<UnmapFn>(_context, 15);

        IsSupported = true;
    }

    // ── Capture ────────────────────────────────────────────────────────────

    public SKBitmap? Capture()
    {
        if (!IsSupported) return null;
        var bmp = new SKBitmap(_width, _height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        CaptureInto(bmp);
        return bmp;
    }

    public void CaptureInto(SKBitmap target)
    {
        if (!IsSupported || _acquire is null) return;

        int hr = _acquire(_duplication, 50, out _, out var res);

        if (hr == DXGI_ERROR_WAIT_TIMEOUT) return; // no new frame — keep previous buffer content
        if (hr == DXGI_ERROR_ACCESS_LOST)  { Reinit(); return; }
        if (hr < 0 || res == IntPtr.Zero)  return;

        try
        {
            var tex = QI(res, IID_ID3D11Texture2D);
            if (tex == IntPtr.Zero) return;
            try
            {
                _copy!(_context, _stagingTex, tex);

                if (_map!(_context, _stagingTex, 0, 1 /*D3D11_MAP_READ*/, 0, out var mapped) < 0)
                    return;
                try
                {
                    var dst      = target.GetPixels();
                    int rowBytes = _width * 4;

                    for (int y = 0; y < _height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(mapped.pData, y * mapped.RowPitch), _rowBuf, 0, rowBytes);
                        Marshal.Copy(_rowBuf, 0, IntPtr.Add(dst, y * rowBytes), rowBytes);
                    }
                }
                finally { _unmap!(_context, _stagingTex, 0); }
            }
            finally { Release(ref tex); }
        }
        finally
        {
            Release(ref res);
            _releaseFrame!(_duplication);
        }
    }

    public void SetMonitor(int monitorIndex, int x, int y, int width, int height)
    {
        _monitorIndex = monitorIndex;
        _width  = width;
        _height = height;
        try { InitOutput(); } catch { IsSupported = false; }
    }

    void Reinit()
    {
        IsSupported = false;
        Release(ref _duplication);
        Release(ref _stagingTex);
        Release(ref _context);
        Release(ref _device);
        _acquire = null; _releaseFrame = null;
        _copy    = null; _map          = null; _unmap = null;
        try { Init(); } catch { }
    }

    public void Dispose()
    {
        Release(ref _duplication);
        Release(ref _stagingTex);
        Release(ref _context);
        Release(ref _device);
    }
}
