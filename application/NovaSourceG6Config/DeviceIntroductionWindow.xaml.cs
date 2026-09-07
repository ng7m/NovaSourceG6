using System.Windows;

namespace NovaSourceG6Config;

public partial class DeviceIntroductionWindow : Window
{
    public DeviceIntroductionWindow()
    {
        InitializeComponent();
        DialogPlacement.Configure(this);
    }
}
