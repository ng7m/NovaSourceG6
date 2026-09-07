using System.Runtime.InteropServices;
using System.Windows;

namespace NovaSourceG6Config;

public partial class TechnicalDetailsWindow : Window
{
    public TechnicalDetailsWindow(string details)
    {
        InitializeComponent();
        DialogPlacement.Configure(this);
        DetailsText.Text = details;
    }

    private void CopyDetails_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(DetailsText.Text);
            CopyStatus.Text = "Details copied.";
        }
        catch (ExternalException)
        {
            CopyStatus.Text = "Clipboard is busy. Try Copy details again.";
        }
    }
}
