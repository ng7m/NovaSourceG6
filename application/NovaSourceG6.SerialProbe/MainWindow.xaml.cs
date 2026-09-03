using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NovaSourceG6.SerialProbe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly SerialPortProbeService portProbeService = new(new WindowsSerialPortProvider());

    public MainWindow()
    {
        InitializeComponent();
        RefreshPorts();
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void TestPort_Click(object sender, RoutedEventArgs e)
    {
        var result = portProbeService.Probe(PortSelector.SelectedItem as string);
        StatusMessage.Text = result.Message;
    }

    private void RefreshPorts()
    {
        var previousSelection = PortSelector.SelectedItem as string;
        var ports = portProbeService.GetAvailablePorts();
        PortSelector.ItemsSource = ports;
        PortSelector.SelectedItem = ports.FirstOrDefault(port =>
            string.Equals(port, previousSelection, StringComparison.OrdinalIgnoreCase));
        StatusMessage.Text = ports.Count == 0
            ? "No serial ports were found. Connect an instrument or configure a virtual COM port, then refresh."
            : $"Found {ports.Count} serial port(s).";
    }
}
