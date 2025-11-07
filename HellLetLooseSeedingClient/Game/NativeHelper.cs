using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Game;

[SupportedOSPlatform("windows")]
public static partial class NativeHelper
{
    const int INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.U4)]
    private static partial uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);


    public static void BringToFront(Process process)
    {
        SetForegroundWindow(process.MainWindowHandle);
    }

    public static void SendKeyPress(Keys key)
    {
        var keyCode = (ushort)key;

        INPUT[] inputs =
        [
            new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            },
            new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            }
        ];
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}

public enum Keys : ushort
{
    Enter = 0x0D,
}


[StructLayout(LayoutKind.Sequential)]
struct INPUT
{
    public uint type;
    public InputUnion u;
}

[StructLayout(LayoutKind.Explicit)]
struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
    [FieldOffset(0)] public HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
struct KEYBDINPUT
{
    public ushort wVk;            // Virtual Key Code
    public ushort wScan;          // Hardware scan code
    public uint dwFlags;          // Flags (e.g., KEYEVENTF_KEYUP)
    public uint time;             // Timestamp
    public IntPtr dwExtraInfo;    // Extra info
}

[StructLayout(LayoutKind.Sequential)]
struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
struct HARDWAREINPUT
{
    public uint uMsg;
    public ushort wParamL;
    public ushort wParamH;
}