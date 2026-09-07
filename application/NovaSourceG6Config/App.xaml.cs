using System.Configuration;
using System.Data;
using System.Windows;

namespace NovaSourceG6Config;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // The installer checks this named object instead of stopping an active device operation.
    private readonly Mutex installationMutex = new(false, @"Local\NovaSourceG6Config.Running");

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        installationMutex.Dispose();
    }
}

