using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NovaSourceG6Config;

internal static class DialogPlacement
{
    public static void Configure(Window dialog)
    {
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dialog.SourceInitialized += (_, _) =>
        {
            if (dialog.Owner is null) return;
            var owner = new WindowInteropHelper(dialog.Owner).Handle;
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromWindow(owner, 2), ref info)) return;
            var dpi = GetDpiForWindow(owner);
            var scale = (dpi == 0 ? 96 : dpi) / 96d;
            dialog.MaxWidth = (info.Work.Right - info.Work.Left) / scale;
            dialog.MaxHeight = (info.Work.Bottom - info.Work.Top) / scale;
            dialog.MinWidth = Math.Min(dialog.MinWidth, dialog.MaxWidth);
            dialog.MinHeight = Math.Min(dialog.MinHeight, dialog.MaxHeight);
            if (!double.IsNaN(dialog.Width)) dialog.Width = Math.Min(dialog.Width, dialog.MaxWidth);
            if (!double.IsNaN(dialog.Height)) dialog.Height = Math.Min(dialog.Height, dialog.MaxHeight);
        };
        dialog.Loaded += (_, _) =>
        {
            if (dialog.Owner is null) return;
            var owner = new WindowInteropHelper(dialog.Owner).Handle;
            var handle = new WindowInteropHelper(dialog).Handle;
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromWindow(owner, 2), ref info) ||
                !GetWindowRect(owner, out var parent) || !GetWindowRect(handle, out var bounds)) return;
            var width = Math.Min(bounds.Right - bounds.Left, info.Work.Right - info.Work.Left);
            var height = Math.Min(bounds.Bottom - bounds.Top, info.Work.Bottom - info.Work.Top);
            var left = Math.Clamp(parent.Left + (parent.Right - parent.Left - width) / 2, info.Work.Left, info.Work.Right - width);
            var top = Math.Clamp(parent.Top + (parent.Bottom - parent.Top - height) / 2, info.Work.Top, info.Work.Bottom - height);
            SetWindowPos(handle, IntPtr.Zero, left, top, width, height, 0x0014);
        };
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
