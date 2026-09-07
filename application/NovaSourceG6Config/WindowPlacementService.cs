using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace NovaSourceG6Config;

public sealed record ScreenArea(string Device, int Left, int Top, int Width, int Height, uint Dpi, bool Primary);
public sealed record SavedWindowPlacement(string Device, int Left, int Top, int Width, int Height, uint Dpi, bool Maximized);

public static class WindowPlacementPolicy
{
    public static bool IsValid(SavedWindowPlacement? saved) => saved is not null &&
        saved.Width is > 0 and <= 100000 && saved.Height is > 0 and <= 100000 &&
        saved.Left is >= -100000 and <= 100000 && saved.Top is >= -100000 and <= 100000 &&
        saved.Dpi is >= 48 and <= 960;

    public static SavedWindowPlacement Resolve(SavedWindowPlacement? saved, IReadOnlyList<ScreenArea> screens)
    {
        var primary = screens.FirstOrDefault(s => s.Primary) ?? screens.First();
        if (!IsValid(saved)) saved = null;
        var screen = saved is null ? primary : screens.FirstOrDefault(s => s.Device == saved.Device) ?? primary;
        if (saved is not null && !screens.Any(s => (long)saved.Left + saved.Width > s.Left &&
            saved.Left < (long)s.Left + s.Width && (long)saved.Top + saved.Height > s.Top && saved.Top < (long)s.Top + s.Height))
            screen = primary;
        var scale = screen.Dpi / (double)(saved?.Dpi ?? 96);
        var width = Math.Clamp((int)Math.Round((saved?.Width ?? 520) * scale), Math.Min(320, screen.Width), screen.Width);
        var height = Math.Clamp((int)Math.Round((saved?.Height ?? 690) * scale), Math.Min(240, screen.Height), screen.Height);
        var intersects = saved is not null && (long)saved.Left + saved.Width > screen.Left &&
            saved.Left < (long)screen.Left + screen.Width && (long)saved.Top + saved.Height > screen.Top &&
            saved.Top < (long)screen.Top + screen.Height;
        var left = intersects ? saved!.Left : screen.Left + (screen.Width - width) / 2;
        var top = intersects ? saved!.Top : screen.Top + (screen.Height - height) / 2;
        return new(screen.Device, Math.Clamp(left, screen.Left, screen.Left + screen.Width - width),
            Math.Clamp(top, screen.Top, screen.Top + screen.Height - height), width, height, screen.Dpi, saved?.Maximized ?? false);
    }
}

public sealed class WindowPlacementService : IDisposable
{
    private readonly Window window;
    private readonly IntPtr handle;
    private readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaSourceG6Config", "window.json");
    private SavedWindowPlacement? normalPlacement;
    private bool maximized;
    private bool restoring;
    private SavedWindowPlacement? startupPlacement;
    private DispatcherOperation? pendingRestore;
    private bool disposed;
    private bool startupCloaked;

    public WindowPlacementService(Window window, IntPtr handle)
    {
        this.window = window;
        this.handle = handle;
        window.LocationChanged += Capture;
        window.SizeChanged += Capture;
        window.StateChanged += Capture;
    }

    public void Restore()
    {
        SavedWindowPlacement? saved = null;
        try { saved = JsonSerializer.Deserialize<SavedWindowPlacement>(File.ReadAllText(path)); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        startupPlacement = saved;
        restoring = true;
        // Hide the entire native window, including its frame, while still allowing
        // WPF/DWM to render and finish the existing DPI-aware restoration sequence.
        var cloak = 1;
        startupCloaked = DwmSetWindowAttribute(handle, 13 /* DWMWA_CLOAK */, ref cloak, sizeof(int)) == 0;
        window.ContentRendered += CompleteRestore;
    }

    private void CompleteRestore(object? sender, EventArgs e)
    {
        window.ContentRendered -= CompleteRestore;
        // SourceInitialized and Loaded still precede WPF's initial show/size work.
        // Wait until that work is complete before applying native screen bounds.
        pendingRestore = window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (disposed) return;
            try { ApplyStartupPlacement(); }
            catch
            {
                FinishRestore();
                throw;
            }
            // Moving between monitors queues WPF DPI/layout work. Apply the final
            // bounds once that work has run, without rewriting WPF Width/Height.
            pendingRestore = window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                if (disposed) return;
                try
                {
                    ApplyStartupPlacement();
                    RecordRestore();
                    if (maximized) window.WindowState = WindowState.Maximized;
                }
                finally { FinishRestore(); }
            }));
        }));
    }

    private void FinishRestore()
    {
        restoring = false;
        startupPlacement = null;
        pendingRestore = null;
        if (!startupCloaked) return;
        var cloak = 0;
        DwmSetWindowAttribute(handle, 13 /* DWMWA_CLOAK */, ref cloak, sizeof(int));
        startupCloaked = false;
    }

    private void ApplyStartupPlacement()
    {
        var screens = GetScreens();
        if (screens.Count == 0) return;
        var placement = WindowPlacementPolicy.Resolve(startupPlacement, screens);
        var screen = screens.First(s => s.Device == placement.Device);

        // Move to the target monitor first, then use the actual window DPI rather
        // than relying on a monitor query made while the window was elsewhere.
        SetWindowPos(handle, IntPtr.Zero, placement.Left, placement.Top, placement.Width, placement.Height, 0x0014);
        var dpi = GetDpiForWindow(handle);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>(), Device = string.Empty };
        if (dpi > 0 && GetMonitorInfo(MonitorFromWindow(handle, 2), ref monitorInfo) && monitorInfo.Device == screen.Device)
        {
            screen = screen with { Dpi = dpi };
            screens = screens.Select(s => s.Device == screen.Device ? screen : s).ToList();
            placement = WindowPlacementPolicy.Resolve(startupPlacement, screens);
        }

        // Let WPF observe WM_MOVE/WM_SIZE from the native operation. Assigning
        // Width/Height here uses its potentially stale source-monitor transform.
        window.MinWidth = Math.Min(320, screen.Width * 96d / placement.Dpi);
        window.MinHeight = Math.Min(240, screen.Height * 96d / placement.Dpi);
        SetWindowPos(handle, IntPtr.Zero, placement.Left, placement.Top, placement.Width, placement.Height, 0x0014);
        normalPlacement = placement with { Maximized = false };
        maximized = placement.Maximized;
    }

    private void RecordRestore()
    {
        if (!GetWindowRect(handle, out var actual)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "window-restore.json"),
                JsonSerializer.Serialize(new
                {
                    Saved = startupPlacement,
                    Requested = normalPlacement,
                    Actual = new { actual.Left, actual.Top, Width = actual.Right - actual.Left,
                        Height = actual.Bottom - actual.Top, Dpi = GetDpiForWindow(handle) },
                    Screens = GetScreens()
                }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private void Capture(object? sender, EventArgs e)
    {
        if (restoring || !window.IsLoaded || IsIconic(handle)) return;
        maximized = IsZoomed(handle);
        if (maximized || !GetWindowRect(handle, out var rect)) return;
        var monitor = MonitorFromWindow(handle, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>(), Device = string.Empty };
        if (!GetMonitorInfo(monitor, ref info)) return;
        normalPlacement = new(info.Device, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, GetDpiForWindow(handle), false);
    }

    public void Save()
    {
        // Closing before restoration finishes must not replace valid saved bounds
        // with the temporary primary-monitor startup window.
        if (restoring) return;
        Capture(null, EventArgs.Empty);
        if (!WindowPlacementPolicy.IsValid(normalPlacement)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(normalPlacement! with { Maximized = maximized }));
            File.Move(path + ".tmp", path, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    public void Dispose()
    {
        disposed = true;
        pendingRestore?.Abort();
        window.ContentRendered -= CompleteRestore;
        window.LocationChanged -= Capture;
        window.SizeChanged -= Capture;
        window.StateChanged -= Capture;
    }

    private static List<ScreenArea> GetScreens()
    {
        var screens = new List<ScreenArea>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr dc, ref NativeRect bounds, IntPtr data) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>(), Device = string.Empty };
            if (GetMonitorInfo(monitor, ref info))
            {
                if (GetDpiForMonitor(monitor, 0, out var dpi, out _) != 0 || dpi == 0) dpi = 96;
                screens.Add(new(info.Device, info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left,
                    info.Work.Bottom - info.Work.Top, dpi, (info.Flags & 1) != 0));
            }
            return true;
        }, IntPtr.Zero);
        return screens;
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, Work;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
    }
    private delegate bool MonitorCallback(IntPtr monitor, IntPtr dc, ref NativeRect bounds, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorCallback callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr window);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
