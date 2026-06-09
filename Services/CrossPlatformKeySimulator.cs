using System.Runtime.InteropServices;
using CrowdKeys.Models;

namespace CrowdKeys.Services;

public class CrossPlatformKeySimulator : IKeySimulator
{
    public void PressKey(string key) => PressCombo([key]);

    public void PressCombo(IReadOnlyList<string> keys)
    {
        if (keys.Count == 0) 
            return;
        
        if (OperatingSystem.IsWindows())      
            WindowsPressCombo(keys);
        else if (OperatingSystem.IsMacOS())   
            MacOsPressCombo(keys);
        else                                  
            LinuxPressCombo(keys);
    }

    public void ClickMouse(MouseButton button, int repeatCount = 1)
    {
        if (OperatingSystem.IsWindows())      
            WindowsClickMouse(button, repeatCount);
        else if (OperatingSystem.IsMacOS())   
            MacOsClickMouse(button, repeatCount);
        else                                 
            LinuxClickMouse(button, repeatCount);
    }

    public void ScrollMouse(ScrollDirection direction, int amount)
    {
        if (OperatingSystem.IsWindows())      
            WindowsScrollMouse(direction, amount);
        else if (OperatingSystem.IsMacOS())   
            MacOsScrollMouse(direction, amount);        
        else                                  
            LinuxScrollMouse(direction, amount);
    }

    public void MoveMouse(int deltaX, int deltaY)
    {
        if (OperatingSystem.IsWindows())      
            WindowsMoveMouse(deltaX, deltaY);
        else if (OperatingSystem.IsMacOS())   
            MacOsMoveMouse(deltaX, deltaY);                    
        else                                 
            LinuxMoveMouse(deltaX, deltaY);
    }

    // ── Windows keyboard ──────────────────────────────────────────────────────

    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nint dwExtraInfo);
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void WindowsPressCombo(IReadOnlyList<string> keys)
    {
        var vkCodes = keys.Select(WindowsVkCode).Where(vk => vk != 0).ToList();
        foreach (var vk in vkCodes)              
            keybd_event(vk, 0, 0, 0);
        
        foreach (var vk in Enumerable.Reverse(vkCodes)) 
            keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
    }

    private static byte WindowsVkCode(string key) => key.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => 0xA2, "SHIFT" => 0xA0, "ALT" => 0xA4, "WIN" => 0x5B,
        "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44, "E" => 0x45,
        "F" => 0x46, "G" => 0x47, "H" => 0x48, "I" => 0x49, "J" => 0x4A,
        "K" => 0x4B, "L" => 0x4C, "M" => 0x4D, "N" => 0x4E, "O" => 0x4F,
        "P" => 0x50, "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
        "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58, "Y" => 0x59, "Z" => 0x5A,
        "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33, "4" => 0x34,
        "5" => 0x35, "6" => 0x36, "7" => 0x37, "8" => 0x38, "9" => 0x39,
        "F1"  => 0x70, "F2"  => 0x71, "F3"  => 0x72, "F4"  => 0x73,
        "F5"  => 0x74, "F6"  => 0x75, "F7"  => 0x76, "F8"  => 0x77,
        "F9"  => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
        "SPACE" or " " => 0x20, "ENTER" => 0x0D, "TAB" => 0x09, "ESCAPE" or "ESC" => 0x1B,
        "LEFT" => 0x25, "UP" => 0x26, "RIGHT" => 0x27, "DOWN" => 0x28,
        "NUMPAD0" => 0x60, "NUMPAD1" => 0x61, "NUMPAD2" => 0x62, "NUMPAD3" => 0x63,
        "NUMPAD4" => 0x64, "NUMPAD5" => 0x65, "NUMPAD6" => 0x66, "NUMPAD7" => 0x67,
        "NUMPAD8" => 0x68, "NUMPAD9" => 0x69,
        _ => 0
    };

    // ── Windows mouse ─────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, nint dwExtraInfo);

    private const uint MOUSEEVENTF_MOVE        = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP      = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN   = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP     = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN  = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP    = 0x0040;
    private const uint MOUSEEVENTF_WHEEL       = 0x0800;
    private const int  WHEEL_DELTA             = 120;

    private static void WindowsClickMouse(MouseButton button, int repeatCount)
    {
        var (down, up) = button switch
        {
            MouseButton.Right  => (MOUSEEVENTF_RIGHTDOWN,  MOUSEEVENTF_RIGHTUP),
            MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _                  => (MOUSEEVENTF_LEFTDOWN,   MOUSEEVENTF_LEFTUP),
        };
        for (var i = 0; i < repeatCount; i++)
        {
            mouse_event(down, 0, 0, 0, 0);
            mouse_event(up,   0, 0, 0, 0);
        }
    }

    private static void WindowsScrollMouse(ScrollDirection direction, int amount)
    {
        var delta = direction == ScrollDirection.Up ? WHEEL_DELTA * amount : -WHEEL_DELTA * amount;
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta, 0);
    }

    private static void WindowsMoveMouse(int deltaX, int deltaY)
        => mouse_event(MOUSEEVENTF_MOVE, deltaX, deltaY, 0, 0);

    // ── macOS keyboard ────────────────────────────────────────────────────────

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventCreateKeyboardEvent(nint source, ushort virtualKey, bool keyDown);
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventPost(uint tap, nint @event);
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventSetFlags(nint @event, ulong flags);
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint cf);
    private const uint kCGHIDEventTap = 0;

    private static bool IsMacModifier(string key) => key.ToUpperInvariant() is
        "CTRL" or "CONTROL" or "SHIFT" or "ALT" or "OPT" or "OPTION" or "CMD" or "COMMAND";

    private static ulong MacModifierFlags(IEnumerable<string> keys)
    {
        ulong flags = 0;
        foreach (var key in keys)
            flags |= key.ToUpperInvariant() switch
            {
                "SHIFT"                          => 0x20000UL,
                "CTRL" or "CONTROL"              => 0x40000UL,
                "ALT" or "OPT" or "OPTION"       => 0x80000UL,
                "CMD" or "COMMAND"               => 0x100000UL,
                _ => 0UL
            };
        return flags;
    }

    private static void MacOsPressCombo(IReadOnlyList<string> keys)
    {
        var modifiers = keys.Where(IsMacModifier).ToList();
        var mainKeys  = keys.Where(k => !IsMacModifier(k)).ToList();
        var flags     = MacModifierFlags(modifiers);

        foreach (var mod in modifiers)
        {
            var code = MacVkCode(mod);
            if (code == ushort.MaxValue) 
                continue;
            
            var ev = CGEventCreateKeyboardEvent(0, code, true);
            CGEventPost(kCGHIDEventTap, ev); CFRelease(ev);
        }
        foreach (var key in mainKeys)
        {
            var code = MacVkCode(key);
            if (code == ushort.MaxValue) 
                continue;
            
            var down = CGEventCreateKeyboardEvent(0, code, true);
            if (flags != 0) 
                CGEventSetFlags(down, flags);
            
            CGEventPost(kCGHIDEventTap, down); CFRelease(down);
            var up = CGEventCreateKeyboardEvent(0, code, false);
            if (flags != 0) 
                CGEventSetFlags(up, flags);
            
            CGEventPost(kCGHIDEventTap, up); CFRelease(up);
        }
        foreach (var mod in Enumerable.Reverse(modifiers))
        {
            var code = MacVkCode(mod);
            if (code == ushort.MaxValue) 
                continue;
            
            var ev = CGEventCreateKeyboardEvent(0, code, false);
            CGEventPost(kCGHIDEventTap, ev); CFRelease(ev);
        }
    }

    private static ushort MacVkCode(string key) => key.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => 0x3B, "SHIFT" => 0x38,
        "ALT" or "OPT" or "OPTION" => 0x3A, "CMD" or "COMMAND" or "WIN" => 0x37,
        "A" => 0x00, "S" => 0x01, "D" => 0x02, "F" => 0x03, "H" => 0x04,
        "G" => 0x05, "Z" => 0x06, "X" => 0x07, "C" => 0x08, "V" => 0x09,
        "B" => 0x0B, "Q" => 0x0C, "W" => 0x0D, "E" => 0x0E, "R" => 0x0F,
        "Y" => 0x10, "T" => 0x11, "1" => 0x12, "2" => 0x13, "3" => 0x14,
        "4" => 0x15, "6" => 0x16, "5" => 0x17, "9" => 0x19, "7" => 0x1A,
        "8" => 0x1C, "0" => 0x1D, "O" => 0x1F, "U" => 0x20, "I" => 0x22,
        "P" => 0x23, "L" => 0x25, "J" => 0x26, "K" => 0x28, "N" => 0x2D, "M" => 0x2E,
        "F1"  => 0x7A, "F2"  => 0x78, "F3"  => 0x63, "F4"  => 0x76,
        "F5"  => 0x60, "F6"  => 0x61, "F7"  => 0x62, "F8"  => 0x64,
        "F9"  => 0x65, "F10" => 0x6D, "F11" => 0x67, "F12" => 0x6F,
        "SPACE" or " " => 0x31, "ENTER" => 0x24, "TAB" => 0x30, "ESCAPE" or "ESC" => 0x35,
        "LEFT" => 0x7B, "RIGHT" => 0x7C, "DOWN" => 0x7D, "UP" => 0x7E,
        _ => ushort.MaxValue
    };

    // ── macOS mouse ───────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint { public double X; public double Y; }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventCreate(nint source);
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CGPoint CGEventGetLocation(nint @event);
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventCreateMouseEvent(nint source, uint mouseType, CGPoint mouseCursorPosition, uint mouseButton);
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventCreateScrollWheelEvent(nint source, uint units, uint wheelCount, int wheel1);

    private const uint kCGEventLeftMouseDown   = 1;
    private const uint kCGEventLeftMouseUp     = 2;
    private const uint kCGEventRightMouseDown  = 3;
    private const uint kCGEventRightMouseUp    = 4;
    private const uint kCGEventMouseMoved      = 5;
    private const uint kCGEventOtherMouseDown  = 25;
    private const uint kCGEventOtherMouseUp    = 26;
    private const uint kCGScrollEventUnitLine  = 1;

    private static CGPoint GetMousePosition()
    {
        var ev  = CGEventCreate(0);
        var pos = CGEventGetLocation(ev);
        CFRelease(ev);
        return pos;
    }

    private static void MacOsClickMouse(MouseButton button, int repeatCount)
    {
        var pos = GetMousePosition();
        var (downType, upType, btn) = button switch
        {
            MouseButton.Right  => (kCGEventRightMouseDown,  kCGEventRightMouseUp,  1u),
            MouseButton.Middle => (kCGEventOtherMouseDown,  kCGEventOtherMouseUp,  2u),
            _                  => (kCGEventLeftMouseDown,   kCGEventLeftMouseUp,   0u),
        };
        for (var i = 0; i < repeatCount; i++)
        {
            var down = CGEventCreateMouseEvent(0, downType, pos, btn);
            CGEventPost(kCGHIDEventTap, down); CFRelease(down);
        
            var up = CGEventCreateMouseEvent(0, upType, pos, btn);
            CGEventPost(kCGHIDEventTap, up); CFRelease(up);
        }
    }

    private static void MacOsScrollMouse(ScrollDirection direction, int amount)
    {
        var delta = direction == ScrollDirection.Up ? amount : -amount;
        var ev = CGEventCreateScrollWheelEvent(0, kCGScrollEventUnitLine, 1, delta);
    
        CGEventPost(kCGHIDEventTap, ev); CFRelease(ev);
    }

    private static void MacOsMoveMouse(int deltaX, int deltaY)
    {
        var pos = GetMousePosition();
        pos.X += deltaX;
        pos.Y += deltaY;
        var ev = CGEventCreateMouseEvent(0, kCGEventMouseMoved, pos, 0);
        CGEventPost(kCGHIDEventTap, ev); CFRelease(ev);
    }

    // ── Linux ─────────────────────────────────────────────────────────────────

    private static void LinuxPressCombo(IReadOnlyList<string> keys)
    {
        try { 
            System.Diagnostics.Process.Start("xdotool", $"key {string.Join("+", keys.Select(k => k.ToLower()))}"); 
        }
        catch { }
    }

    private static void LinuxClickMouse(MouseButton button, int repeatCount)
    {
        var btn = button switch { MouseButton.Right => "3", MouseButton.Middle => "2", _ => "1" };
        for (var i = 0; i < repeatCount; i++)
        {
            try { 
                System.Diagnostics.Process.Start("xdotool", $"click {btn}"); 
            } 
            catch { }
        }
    }

    private static void LinuxScrollMouse(ScrollDirection direction, int amount)
    {
        var btn = direction == ScrollDirection.Up ? "4" : "5";
        for (var i = 0; i < amount; i++)
        {
            try 
            { 
                System.Diagnostics.Process.Start("xdotool", $"click {btn}"); 
            } 
            catch { }
        }
    }

    private static void LinuxMoveMouse(int deltaX, int deltaY)
    {
        try 
        { 
            System.Diagnostics.Process.Start("xdotool", $"mousemove_relative -- {deltaX} {deltaY}"); 
        } catch { }
    }
}
