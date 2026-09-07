using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Navigation;

namespace NovaSourceG6Config;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        SourceInitialized += ConfigureOwnerMonitor;
        ContentRendered += CenterOnOwnerMonitor;
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null
            ? "Version unavailable"
            : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    private void ConfigureOwnerMonitor(object? sender, EventArgs e)
    {
        if (Owner is null) return;
        var ownerHandle = new WindowInteropHelper(Owner).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(ownerHandle, 2), ref info)) return;
        var dpi = GetDpiForWindow(ownerHandle);
        var scale = (dpi == 0 ? 96 : dpi) / 96d;
        // Grow to the measured content height, with scrolling only when the
        // owner's display is too small to contain the full description.
        MaxWidth = (info.Work.Right - info.Work.Left) / scale;
        MaxHeight = (info.Work.Bottom - info.Work.Top) / scale;
        MinWidth = Math.Min(MinWidth, MaxWidth);
        MinHeight = Math.Min(MinHeight, MaxHeight);
        Width = Math.Min(740, MaxWidth);
    }

    private void CenterOnOwnerMonitor(object? sender, EventArgs e)
    {
        ContentRendered -= CenterOnOwnerMonitor;
        if (Owner is null) return;
        var ownerHandle = new WindowInteropHelper(Owner).Handle;
        var handle = new WindowInteropHelper(this).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(ownerHandle, 2), ref info) ||
            !GetWindowRect(ownerHandle, out var ownerBounds) || !GetWindowRect(handle, out var bounds)) return;
        var width = Math.Min(bounds.Right - bounds.Left, info.Work.Right - info.Work.Left);
        var height = Math.Min(bounds.Bottom - bounds.Top, info.Work.Bottom - info.Work.Top);
        var left = Math.Clamp(ownerBounds.Left + (ownerBounds.Right - ownerBounds.Left - width) / 2,
            info.Work.Left, info.Work.Right - width);
        var top = Math.Clamp(ownerBounds.Top + (ownerBounds.Bottom - ownerBounds.Top - height) / 2,
            info.Work.Top, info.Work.Bottom - height);
        SetWindowPos(handle, IntPtr.Zero, left, top, width, height, 0x0014);
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, Work;
        public uint Flags;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

    private void Email_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        try
        {
            Process.Start(new ProcessStartInfo("mailto:ng7m@arrl.net") { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, "Could not open your email application. You can email Max at ng7m@arrl.net.",
                "Email", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ViewIntroduction_Click(object sender, RoutedEventArgs e) =>
        new DeviceIntroductionWindow { Owner = this }.ShowDialog();

    private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("http://www.ng7m.com/downloads/NG7M/NovaSourceG6/") { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, "Could not open your browser. Visit http://www.ng7m.com/downloads/NG7M/NovaSourceG6/ to check for updates.",
                "Check for updates", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ViewLicense_Click(object sender, RoutedEventArgs e)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NovaSourceG6Config.LICENSE")!;
        using var reader = new StreamReader(stream);
        var text = new TextBox
        {
            Text = FormatLicenseForDisplay(reader.ReadToEnd()), IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(16)
        };
        text.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush");
        text.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        var close = new Button { Content = "Close", IsCancel = true, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16), MinWidth = 90 };
        var panel = new DockPanel();
        DockPanel.SetDock(close, Dock.Bottom);
        panel.Children.Add(close);
        panel.Children.Add(text);
        var dialog = new Window
        {
            Title = "GNU General Public License — version 3", Owner = this, ShowInTaskbar = false,
            Width = Math.Min(600, SystemParameters.WorkArea.Width), Height = Math.Min(650, SystemParameters.WorkArea.Height),
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel
        };
        dialog.SetResourceReference(BackgroundProperty, "AppBackgroundBrush");
        DialogPlacement.Configure(dialog);
        dialog.ShowDialog();
    }

    private static string FormatLicenseForDisplay(string license)
    {
        // Reflow prose only in the viewer; retain paragraph and notice boundaries.
        var output = new System.Text.StringBuilder();
        string? previous = null;
        using var lines = new StringReader(license);
        while (lines.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (previous is not null)
            {
                var previousText = previous.Trim();
                var keepBreak = trimmed.Length == 0 || previousText.Length == 0 ||
                    line.StartsWith("          ", StringComparison.Ordinal) ||
                    previous.StartsWith("          ", StringComparison.Ordinal) ||
                    trimmed.StartsWith("Copyright (C)", StringComparison.Ordinal) ||
                    previousText.Contains("Copyright (C)", StringComparison.Ordinal);
                output.Append(keepBreak ? Environment.NewLine : " ");
            }
            output.Append(trimmed);
            previous = line;
        }
        return output.ToString();
    }
}
