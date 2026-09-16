using System.Runtime.InteropServices;

namespace AnalysisReviewControl;

internal static class NativeMethods
{
    public const uint LsfwLock = 1;
    public const uint LsfwUnlock = 2;

    [DllImport("user32.dll")]
    public static extern bool LockSetForegroundWindow(uint uLockCode);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
