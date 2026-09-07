using System.Windows;
using System.Windows.Controls;

namespace NovaSourceG6Config;

public partial class TriggerWindow : Window
{
    private readonly G6DeviceService deviceService;
    private readonly G6DeviceState originalState;
    private bool isInitializing = true;
    private bool isApplying;

    public G6DeviceState? UpdatedState { get; private set; }
    public bool StateUncertain { get; private set; }

    public TriggerWindow(G6DeviceService deviceService, G6DeviceState originalState)
    {
        InitializeComponent();
        DialogPlacement.Configure(this);
        Closing += (_, e) => { if (isApplying && UpdatedState is null && !StateUncertain) e.Cancel = true; };
        this.deviceService = deviceService;
        this.originalState = originalState;
        SelectTag(SourceSelector, originalState.InputMode);
        SelectTag(ModeSelector, originalState.TriggerMode);
        InternalTriggerCheckBox.IsChecked = originalState.InternalTriggerEnabled;
        RfStandbyCheckBox.IsChecked = originalState.RfStandbyEnabled;
        isInitializing = false;
        UpdateControls();
    }

    private void Control_Changed(object sender, RoutedEventArgs e)
    {
        if (isInitializing) return;
        if (ReferenceEquals(sender, SourceSelector) && SelectedTag(SourceSelector) == "T" &&
            originalState.ModulationSource == "E")
        {
            MessageBox.Show(this,
                "External modulation currently uses the shared rear input. Applying External trigger will turn External modulation off.",
                "Shared rear input", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        UpdateControls();
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        var updated = CreateUpdatedState();
        isApplying = true;
        UpdateControls();
        try
        {
            await deviceService.ApplyChangesAsync(originalState, updated);
            UpdatedState = updated;
            DialogResult = true;
        }
        catch (Exception exception)
        {
            StateUncertain = true;
            MessageBox.Show(this, $"The trigger settings could not be applied: {exception.Message}",
                "Trigger error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        var internalSource = SelectedTag(SourceSelector) == "M";
        SourceSelector.IsEnabled = !isApplying;
        ModeSelector.IsEnabled = !isApplying;
        InternalTriggerCheckBox.IsEnabled = internalSource && !isApplying;
        RfStandbyCheckBox.IsEnabled = !isApplying;
        ApplyButton.IsEnabled = !isApplying && CreateUpdatedState() != originalState;
    }

    private G6DeviceState CreateUpdatedState()
    {
        var inputMode = SelectedTag(SourceSelector);
        return originalState with
        {
            InputMode = inputMode,
            TriggerMode = SelectedTag(ModeSelector),
            InternalTriggerEnabled = InternalTriggerCheckBox.IsChecked == true,
            RfStandbyEnabled = RfStandbyCheckBox.IsChecked == true,
            ModulationSource = inputMode == "T" && originalState.ModulationSource == "E"
                ? "N"
                : originalState.ModulationSource
        };
    }

    private static string SelectedTag(ComboBox box) =>
        (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

    private static void SelectTag(ComboBox box, string tag) =>
        box.SelectedItem = box.Items.Cast<ComboBoxItem>().First(item => item.Tag?.ToString() == tag);
}
