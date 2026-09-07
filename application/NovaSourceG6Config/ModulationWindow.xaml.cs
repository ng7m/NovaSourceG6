using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace NovaSourceG6Config;

public partial class ModulationWindow : Window
{
    private readonly G6DeviceService deviceService;
    private readonly G6DeviceState originalState;
    private bool isInitializing = true;
    private bool isApplying;

    public G6DeviceState? UpdatedState { get; private set; }
    public bool StateUncertain { get; private set; }

    public ModulationWindow(G6DeviceService deviceService, G6DeviceState originalState)
    {
        InitializeComponent();
        DialogPlacement.Configure(this);
        Closing += (_, e) => { if (isApplying && UpdatedState is null && !StateUncertain) e.Cancel = true; };
        this.deviceService = deviceService;
        this.originalState = originalState;
        SourceSelector.SelectedItem = SourceSelector.Items.Cast<ComboBoxItem>()
            .First(item => item.Tag?.ToString() == originalState.ModulationSource);
        GainTextBox.Text = originalState.ModulationGain.ToString(CultureInfo.InvariantCulture);
        isInitializing = false;
        UpdateControls();
    }

    private void SourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializing) return;
        if (SelectedSource == "E" && originalState.InputMode != "M")
        {
            MessageBox.Show(this,
                "Applying External modulation will change the shared rear input to Modulation and disable external triggering.",
                "Shared rear input", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        UpdateControls();
    }

    private void GainTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (!isInitializing) UpdateControls();
    }

    private void GainDown_Click(object sender, RoutedEventArgs e) => StepGain(-1);
    private void GainUp_Click(object sender, RoutedEventArgs e) => StepGain(1);

    private void StepGain(int direction)
    {
        if (!int.TryParse(GainTextBox.Text, out var gain)) gain = originalState.ModulationGain;
        GainTextBox.Text = Math.Clamp(gain + direction, -10, 13).ToString(CultureInfo.InvariantCulture);
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCreateUpdatedState(out var updated)) return;
        isApplying = true;
        UpdateControls();
        try
        {
            await deviceService.ApplyChangesAsync(originalState, updated!);
            UpdatedState = updated;
            DialogResult = true;
        }
        catch (Exception exception)
        {
            StateUncertain = true;
            MessageBox.Show(this, $"The modulation settings could not be applied: {exception.Message}",
                "Modulation error", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
        }
        finally
        {
            isApplying = false;
            if (IsVisible) UpdateControls();
        }
    }

    private void UpdateControls()
    {
        var gainEnabled = SelectedSource != "N" && !isApplying;
        GainTextBox.IsEnabled = gainEnabled;
        GainDownButton.IsEnabled = gainEnabled;
        GainUpButton.IsEnabled = gainEnabled;
        SourceSelector.IsEnabled = !isApplying;

        if (int.TryParse(GainTextBox.Text, out var gain) && gain is >= -10 and <= 13)
        {
            MultiplierText.Text = $"Gain {gain:+0;-0;0} = {Math.Pow(2, gain):0.########}× modulation";
            ApplyButton.IsEnabled = !isApplying && TryCreateUpdatedState(out var updated) && updated != originalState;
        }
        else
        {
            MultiplierText.Text = "Gain must be a whole number from -10 through +13.";
            ApplyButton.IsEnabled = false;
        }
    }

    private bool TryCreateUpdatedState(out G6DeviceState? updated)
    {
        updated = null;
        if (!int.TryParse(GainTextBox.Text, out var gain) || gain is < -10 or > 13) return false;
        var source = SelectedSource;
        if (string.IsNullOrEmpty(source)) return false;
        updated = originalState with
        {
            ModulationSource = source,
            ModulationGain = gain,
            InputMode = source == "E" ? "M" : originalState.InputMode
        };
        return true;
    }

    private string SelectedSource => (SourceSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
}
