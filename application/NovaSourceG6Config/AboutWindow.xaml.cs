using System.Reflection;
using System.Windows;

namespace NovaSourceG6Config;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null
            ? "Version unavailable"
            : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }
}
