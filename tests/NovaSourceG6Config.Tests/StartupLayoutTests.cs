using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public class StartupLayoutTests
{
    [Fact]
    public async Task StartupAndPortSelectionNeverOpenConnectionAndStatusFitsDefaultLayout()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var provider = new NoConnectionProvider();
                var window = new MainWindow(provider, new RememberedPort());
                var selector = (ComboBox)window.FindName("PortSelector");
                Assert.Equal("COM50", selector.SelectedItem);
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                selector.SelectedItem = "COM3";
                Assert.Equal(0, provider.ConnectionsCreated);
                Assert.True(((Button)window.FindName("ConnectionButton")).IsEnabled);
                var content = (ScrollViewer)window.Content;
                content.Measure(new Size(504, 650));
                content.Arrange(new Rect(0, 0, 504, 650));
                content.UpdateLayout();
                Assert.Equal(0, content.ScrollableHeight);
                var status = (TextBlock)window.FindName("StatusMessage");
                Assert.True(status.TransformToAncestor(content).Transform(new Point(0, status.ActualHeight)).Y <= 650);
                RenderIfRequested(content, "main-default", 504, 650);
                content.Measure(new Size(350, 250));
                content.Arrange(new Rect(0, 0, 350, 250));
                content.UpdateLayout();
                Assert.True(content.ScrollableHeight > 0);
                Assert.True(content.ScrollableWidth > 0);
                window.Close();
                var missing = new MainWindow(new NoConnectionProvider(false), new RememberedPort());
                Assert.Null(((ComboBox)missing.FindName("PortSelector")).SelectedItem);
                Assert.False(((Button)missing.FindName("ConnectionButton")).IsEnabled);
                Assert.Contains("unavailable", ((TextBlock)missing.FindName("StatusMessage")).Text);
                missing.Close();
                var about = new AboutWindow();
                Assert.StartsWith("Version ", ((TextBlock)about.FindName("VersionText")).Text);
                RenderIfRequested((FrameworkElement)about.Content, "about", 664, 520);
                app.Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                app.Resources["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                app.Resources["ControlBrush"] = new SolidColorBrush(Color.FromRgb(55, 65, 81));
                app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(75, 85, 99));
                app.Resources["TextBrush"] = Brushes.WhiteSmoke;
                app.Resources["SecondaryTextBrush"] = Brushes.LightGray;
                RenderIfRequested((FrameworkElement)about.Content, "about-dark", 664, 520);
                using var license = typeof(AboutWindow).Assembly.GetManifestResourceStream("NovaSourceG6Config.LICENSE");
                Assert.NotNull(license);
                using var reader = new StreamReader(license!);
                Assert.Contains("END OF TERMS AND CONDITIONS", reader.ReadToEnd());
                about.Close();
                app.Shutdown();
                completion.SetResult();
            }
            catch (Exception exception) { completion.SetException(exception); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    private static void RenderIfRequested(FrameworkElement content, string name, int width, int height)
    {
        var folder = Environment.GetEnvironmentVariable("NSG6_UI_ARTIFACT_DIR");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        foreach (var scale in new[] { 1d, 1.5d, 2d })
        {
            var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            var background = new DrawingVisual();
            using (var drawing = background.RenderOpen()) drawing.DrawRectangle((Brush)Application.Current.Resources["AppBackgroundBrush"], null, new Rect(0, 0, width, height));
            bitmap.Render(background);
            bitmap.Render(content);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(folder, $"{name}-{scale * 100:0}.png"));
            png.Save(output);
        }
    }

    private class RememberedPort : IConnectionProfileStore
    {
        public ConnectionProfile? Load() => new("", "COM50");
        public void Save(ConnectionProfile profile) => throw new InvalidOperationException("Startup must not save a connection.");
    }
    private class NoConnectionProvider(bool rememberedAvailable = true) : ISerialPortProvider
    {
        public int ConnectionsCreated { get; private set; }
        public IReadOnlyList<string> GetPortNames() => rememberedAvailable ? ["COM3", "COM50"] : ["COM3"];
        public ISerialPortConnection CreateConnection(string portName)
        {
            ConnectionsCreated++;
            throw new InvalidOperationException("Startup must not connect.");
        }
    }
}
