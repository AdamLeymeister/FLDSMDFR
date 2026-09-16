using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AnalysisReviewControl;

internal static class NativeMethods
{
    private const byte VkShift = 0x10;
    private const byte VkRight = 0x27;
    private const uint KeyeventfKeyup = 0x0002;

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static bool IsVsCodeForeground()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            string name = Process.GetProcessById((int)processId).ProcessName;
            return name.Contains("Code", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static void SelectNextCharacters(int count)
    {
        int length = Math.Clamp(count, 0, 200);
        if (length == 0)
        {
            return;
        }

        keybd_event(VkShift, 0, 0, UIntPtr.Zero);
        for (int i = 0; i < length; i++)
        {
            keybd_event(VkRight, 0, 0, UIntPtr.Zero);
            keybd_event(VkRight, 0, KeyeventfKeyup, UIntPtr.Zero);
        }

        keybd_event(VkShift, 0, KeyeventfKeyup, UIntPtr.Zero);
    }
}
