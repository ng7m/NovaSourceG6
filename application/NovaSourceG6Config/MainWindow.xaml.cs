using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace NovaSourceG6Config;

public partial class MainWindow : Window
{
    private const int AboutSystemCommand = 0x1000;
    private const int SystemThemeCommand = 0x1010;
    private const int LightThemeCommand = 0x1020;
    private const int DarkThemeCommand = 0x1030;
    private const int WindowSystemCommand = 0x0112;
    private const uint MenuSeparator = 0x0800;
    private const uint MenuString = 0x0000;
    private const uint MenuChecked = 0x0008;
    private readonly SerialPortProbeService portProbeService;
    private readonly ConnectionProfileService connectionProfileService;
    private readonly G6DeviceService deviceService;
    private readonly ThemeService themeService = new();
    private readonly string? rememberedPortName;
    private bool isRefreshingPorts;
    private bool isBusy;
    private bool isConnecting;
    private bool isPopulating;
    private bool hasUnstoredChanges;
    private G6DeviceState? confirmedState;
    private HwndSource? windowSource;
    private IntPtr systemMenuHandle;
    private WindowPlacementService? placementService;
    private bool closeAfterOperation;
    private CancellationTokenSource? statusPollingCancellation;
    private Task? statusPollingTask;
    private bool isClosing;
    private bool closeReady;

    private void StartStatusPolling()
    {
        if (statusPollingTask is { IsCompleted: false } || isClosing) return;
        statusPollingCancellation?.Dispose();
        statusPollingCancellation = new();
        statusPollingTask = PollStatusAsync(statusPollingCancellation.Token);
    }

    private async Task StopStatusPollingAsync()
    {
        statusPollingCancellation?.Cancel();
        if (statusPollingTask is not null) await statusPollingTask;
        statusPollingTask = null;
        statusPollingCancellation?.Dispose();
        statusPollingCancellation = null;
    }

    private async Task PollStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                if (isBusy || confirmedState is null) continue;
                if (!portProbeService.IsConnected) return;
                // Finish an in-flight reply before stopping, so the serial stream stays aligned.
                var status = await deviceService.TryReadStatusAsync();
                if (cancellationToken.IsCancellationRequested) return;
                if (status is null) continue;
                RfIndicator.IsRfOn = status.RfOn;
                LockIndicator.Fill = StatusBrush(status.Locked);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            portProbeService.Disconnect();
            SetDisconnectedVisualState();
            UpdateConnectionControls();
            StatusMessage.Text = $"G6 status is unknown: {exception.Message} Click Connect to reconnect and read the device. Power loss and communication loss cannot be distinguished.";
        }
    }

    public MainWindow() : this(new WindowsSerialPortProvider(), new JsonConnectionProfileStore()) { }

    public MainWindow(ISerialPortProvider provider, IConnectionProfileStore profileStore)
    {
        portProbeService = new(provider);
        connectionProfileService = new(profileStore);
        themeService.LoadAndApply();
        InitializeComponent();
        deviceService = new(portProbeService);
        rememberedPortName = connectionProfileService.Load()?.PortName;
        AttenuationSelector.ItemsSource = Enumerable.Range(0, 32).Select(value => $"{value} step{(value == 1 ? "" : "s")}").ToArray();
        RefreshPorts(rememberedPortName);
        SystemParameters.StaticPropertyChanged += SystemParameters_Changed;
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private string? connectionDetails;

    private void ConnectionDetails_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(connectionDetails))
            new TechnicalDetailsWindow(connectionDetails) { Owner = this }.ShowDialog();
    }

    private void SetConnectionDetails(string? details = null)
    {
        connectionDetails = details;
        ConnectionDetails.Visibility = string.IsNullOrWhiteSpace(details) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PortSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isRefreshingPorts || PortSelector.SelectedItem is not string) return;
        SetConnectionDetails();
        PortSelector.IsDropDownOpen = false;
        StatusMessage.Text = $"{PortSelector.SelectedItem} selected. Click Connect to read the G6.";
        UpdateConnectionControls();
    }

    private async Task ConnectAsync()
    {
        if (isBusy || PortSelector.SelectedItem is not string portName) return;
        isConnecting = true;
        try
        {
            await RunBusyAsync($"Testing {portName} for a NovaSource G6 connection...", async () =>
            {
                var result = await portProbeService.ProbeAsync(portName);
                if (!result.Succeeded)
                {
                    SetDisconnectedVisualState();
                    StatusMessage.Text = result.UserMessage ?? $"Couldn’t connect to the G6 on {portName}. Check the device’s power, serial cable, and selected COM port, then click Connect to try again.";
                    var details = result.Message;
                    if (!string.IsNullOrEmpty(result.RawResponse))
                        details += $"\n\nRaw response:\n{result.RawResponse}";
                    SetConnectionDetails(details);
                    return;
                }
                try
                {
                    isConnecting = false;
                    UpdateConnectionControls();
                    await ReadDeviceStateAsync();
                    var saved = connectionProfileService.Save(string.Empty, portName, "38400");
                    if (!saved.Succeeded) StatusMessage.Text += $" The port could not be remembered: {saved.Message}";
                }
                catch (Exception exception)
                {
                    portProbeService.Disconnect();
                    SetDisconnectedVisualState();
                    StatusMessage.Text = $"The G6 responded on {portName}, but its configuration couldn’t be loaded. Check the device’s power and serial connection, then click Connect to try again.";
                    SetConnectionDetails(exception.Message);
                }
            });
        }
        finally
        {
            isConnecting = false;
            UpdateConnectionControls();
        }
    }

    private async Task ReadDeviceStateAsync()
    {
        StatusMessage.Text = "Reading the current G6 configuration...";
        await Dispatcher.Yield(DispatcherPriority.Render);
        var state = await deviceService.ReadStateAsync();
        confirmedState = state;
        PopulateControls(state);
        StatusMessage.Text = $"Configuration loaded from G6 connected to {portProbeService.ActivePortName}";
        StartStatusPolling();
    }

    private void PopulateControls(G6DeviceState state)
    {
        isPopulating = true;
        try
        {
            FrequencyTextBox.Text = state.FrequencyMhz.ToString("0.000", CultureInfo.InvariantCulture);
            FrequencyRangeText.Text = $"{state.MinimumFrequencyMhz:0.000} - {state.MaximumFrequencyMhz:0.000} MHz";
            AttenuationSelector.SelectedIndex = state.Attenuation;
            RfIndicator.IsRfOn = state.RfOn; LockIndicator.Fill = StatusBrush(state.Locked);
        }
        finally { isPopulating = false; }
        UpdateActionButtons();
    }

    private async void RefreshState_Click(object sender, RoutedEventArgs e) => await RunBusyAsync("Refreshing device state...", ReadDeviceStateAsync);

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildState(out var state, out var error)) { StatusMessage.Text = error; return; }
        var originalState = confirmedState!;
        await RunBusyAsync("Applying configuration...", async () =>
        {
            var applied = await deviceService.ApplyChangesAsync(originalState, state!);
            confirmedState = state;
            hasUnstoredChanges |= applied > 0;
            await ReadDeviceStateAsync();
            StatusMessage.Text = applied == 1
                ? "The changed setting was accepted by the G6"
                : $"{applied} changed settings were accepted by the G6";
        });
    }

    private async void Load_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Load the configuration stored in the G6? Current active values will be replaced.", "Load saved configuration", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        await RunBusyAsync("Loading the saved device configuration...", async () => { await deviceService.LoadAsync(); hasUnstoredChanges = false; await ReadDeviceStateAsync(); });
    }

    private async void Store_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Save the applied configuration as the G6 power-up configuration?", "Save to device memory", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        await RunBusyAsync("Saving the power-up configuration...", async () =>
        {
            await deviceService.StoreAsync();
            hasUnstoredChanges = false;
            UpdateActionButtons();
            StatusMessage.Text = "The applied configuration was saved to G6 nonvolatile memory";
        });
    }

    private async void OpenSweep_Click(object sender, RoutedEventArgs e)
    {
        if (confirmedState is null) return;
        var dialog = new SweepWindow(deviceService, confirmedState.MinimumFrequencyMhz, confirmedState.MaximumFrequencyMhz, confirmedState.FrequencyMhz) { Owner = this };
        dialog.ShowDialog();
        await RefreshAfterDialogAsync(dialog.StateUncertain, dialog.AppliedFrequencyMhz is not null, frequencyOnly: true);
    }

    private async void OpenModulation_Click(object sender, RoutedEventArgs e)
    {
        if (confirmedState is null) return;
        var dialog = new ModulationWindow(deviceService, confirmedState) { Owner = this };
        var changed = dialog.ShowDialog() == true && dialog.UpdatedState is not null;
        await RefreshAfterDialogAsync(dialog.StateUncertain, changed);
    }

    private async void OpenTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (confirmedState is null) return;
        var dialog = new TriggerWindow(deviceService, confirmedState) { Owner = this };
        var changed = dialog.ShowDialog() == true && dialog.UpdatedState is not null;
        await RefreshAfterDialogAsync(dialog.StateUncertain, changed);
    }

    private async Task RefreshAfterDialogAsync(bool uncertain, bool changed, bool frequencyOnly = false)
    {
        if (uncertain || !portProbeService.IsConnected)
        {
            await StopStatusPollingAsync();
            portProbeService.Disconnect();
            SetDisconnectedVisualState();
            UpdateConnectionControls();
            StatusMessage.Text = "Device state is unverified. Some changes may have reached the G6. Click Connect to read its current state.";
            return;
        }
        if (!changed || confirmedState is null) return;
        hasUnstoredChanges = true;
        await RunBusyAsync(frequencyOnly ? "Reading the final frequency..." : "Verifying modulation and trigger settings...", async () =>
        {
            confirmedState = frequencyOnly
                ? await deviceService.ReadFrequencyAsync(confirmedState!)
                : await deviceService.ReadInputSettingsAsync(confirmedState!);
            if (frequencyOnly)
            {
                isPopulating = true;
                try { FrequencyTextBox.Text = confirmedState.FrequencyMhz.ToString("0.000", CultureInfo.InvariantCulture); }
                finally { isPopulating = false; }
            }
            UpdateActionButtons();
            StatusMessage.Text = frequencyOnly
                ? "The final frequency was read from the G6."
                : "The modulation and trigger settings were read back from the G6.";
        });
    }

    private void ShowAbout() => new AboutWindow { Owner = this }.ShowDialog();

    private void About_Click(object sender, RoutedEventArgs e) => ShowAbout();

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        placementService = new WindowPlacementService(this, windowHandle);
        placementService.Restore();
        systemMenuHandle = GetSystemMenu(windowHandle, false);
        if (systemMenuHandle != IntPtr.Zero)
        {
            AppendMenu(systemMenuHandle, MenuSeparator, 0, null);
            AppendMenu(systemMenuHandle, MenuString, SystemThemeCommand, "Theme: Match Windows");
            AppendMenu(systemMenuHandle, MenuString, LightThemeCommand, "Theme: Light");
            AppendMenu(systemMenuHandle, MenuString, DarkThemeCommand, "Theme: Dark");
            AppendMenu(systemMenuHandle, MenuSeparator, 0, null);
            AppendMenu(systemMenuHandle, MenuString, AboutSystemCommand, "About NovaSource G6 Config…");
            UpdateThemeMenuChecks();
        }

        windowSource = HwndSource.FromHwnd(windowHandle);
        windowSource?.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WindowSystemCommand)
        {
            var command = (int)(wParam.ToInt64() & 0xFFF0);
            if (command == AboutSystemCommand)
            {
                ShowAbout();
                handled = true;
            }
            else if (command is SystemThemeCommand or LightThemeCommand or DarkThemeCommand)
            {
                themeService.Select(command switch
                {
                    LightThemeCommand => ApplicationTheme.Light,
                    DarkThemeCommand => ApplicationTheme.Dark,
                    _ => ApplicationTheme.System
                });
                UpdateThemeMenuChecks();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void FrequencyDown_Click(object sender, RoutedEventArgs e) => StepFrequency(-1);
    private void FrequencyUp_Click(object sender, RoutedEventArgs e) => StepFrequency(1);
    private void AttenuationDown_Click(object sender, RoutedEventArgs e) => StepAttenuation(-1);
    private void AttenuationUp_Click(object sender, RoutedEventArgs e) => StepAttenuation(1);

    private void StepAttenuation(int direction)
    {
        var current = AttenuationSelector.SelectedIndex;
        if (current < 0) current = confirmedState?.Attenuation ?? 0;
        AttenuationSelector.SelectedIndex = Math.Clamp(current + direction, 0, 31);
    }

    private void StepFrequency(int direction)
    {
        if (!decimal.TryParse(FrequencyTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var current)) return;
        var step = decimal.Parse(((ComboBoxItem)FrequencyStepSelector.SelectedItem).Tag!.ToString()!, CultureInfo.InvariantCulture);
        if (confirmedState is not null) current = Math.Clamp(current + direction * step, confirmedState.MinimumFrequencyMhz, confirmedState.MaximumFrequencyMhz);
        FrequencyTextBox.Text = current.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private void EditableControl_Changed(object sender, EventArgs e)
    {
        if (isPopulating || !IsLoaded) return;
        UpdateActionButtons();
    }

    private bool TryBuildState(out G6DeviceState? state, out string error)
    {
        state = null; error = string.Empty;
        if (confirmedState is null) { error = "Refresh the device state before applying changes."; return false; }
        if (!decimal.TryParse(FrequencyTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var frequency) || frequency < confirmedState.MinimumFrequencyMhz || frequency > confirmedState.MaximumFrequencyMhz) { error = $"Frequency must be between {confirmedState.MinimumFrequencyMhz:0.000} and {confirmedState.MaximumFrequencyMhz:0.000} MHz."; return false; }
        state = confirmedState with { FrequencyMhz = decimal.Round(frequency, 3), Attenuation = AttenuationSelector.SelectedIndex };
        return true;
    }

    private async Task RunBusyAsync(string message, Func<Task> action)
    {
        if (isBusy) return;
        SetConnectionDetails();
        isBusy = true; StatusMessage.Text = message; UpdateConnectionControls();
        await Dispatcher.Yield(DispatcherPriority.Render);
        try
        {
            await StopStatusPollingAsync();
            await action();
        }
        catch (Exception exception)
        {
            portProbeService.Disconnect();
            SetDisconnectedVisualState();
            StatusMessage.Text = $"Operation failed: {exception.Message} Device state is unverified; click Connect to read it again. Some changes may have reached the G6.";
        }
        finally
        {
            isBusy = false;
            UpdateConnectionControls();
            if (portProbeService.IsConnected && confirmedState is not null) StartStatusPolling();
            if (closeAfterOperation) _ = Dispatcher.BeginInvoke(new Action(Close));
        }
    }

    private async void ConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (!portProbeService.IsConnected)
        {
            await ConnectAsync();
            return;
        }

        var name = portProbeService.ActivePortName;
        SetConnectionDetails();
        isBusy = true;
        UpdateConnectionControls();
        await StopStatusPollingAsync();
        portProbeService.Disconnect();
        SetDisconnectedVisualState();
        isBusy = false;
        UpdateConnectionControls();
        StatusMessage.Text = name is null ? "No serial connection is open." : $"Disconnected from {name}. Click Connect to reconnect.";
        if (closeAfterOperation) Close();
    }
    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (closeReady) return;
        if (isBusy) { e.Cancel = true; closeAfterOperation = true; return; }
        e.Cancel = true;
        if (isClosing) return;
        isClosing = true;
        IsEnabled = false;
        placementService?.Save();
        await StopStatusPollingAsync();
        closeReady = true;
        _ = Dispatcher.BeginInvoke(new Action(Close));
    }
    private void Window_Closed(object? sender, EventArgs e)
    {
        placementService?.Dispose();
        SystemParameters.StaticPropertyChanged -= SystemParameters_Changed;
        windowSource?.RemoveHook(WindowMessageHook);
        portProbeService.Dispose();
    }
    private void SystemParameters_Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => themeService.RefreshSystemTheme();
    private void UpdateThemeMenuChecks()
    {
        if (systemMenuHandle == IntPtr.Zero) return;
        CheckMenuItem(systemMenuHandle, SystemThemeCommand, themeService.Current == ApplicationTheme.System ? MenuChecked : 0);
        CheckMenuItem(systemMenuHandle, LightThemeCommand, themeService.Current == ApplicationTheme.Light ? MenuChecked : 0);
        CheckMenuItem(systemMenuHandle, DarkThemeCommand, themeService.Current == ApplicationTheme.Dark ? MenuChecked : 0);
    }
    private void UpdateConnectionControls()
    {
        var connected = portProbeService.IsConnected;
        PortSelector.IsEnabled = !connected && !isBusy;
        RefreshPortsButton.IsEnabled = !connected && !isBusy;
        ConnectionButton.Content = isConnecting ? "Connecting…" : connected ? "Disconnect" : "Connect";
        ConnectionButton.IsEnabled = !isBusy && (connected || PortSelector.SelectedItem is string);
        DevicePanel.IsEnabled = connected && !isBusy;
        ConnectionIndicator.Fill = connected ? Brushes.SeaGreen : Brushes.Red; ConnectionStateText.Text = connected ? $"Connected · {portProbeService.ActivePortName}" : "Disconnected";
    }
    private void SetDisconnectedVisualState()
    {
        statusPollingCancellation?.Cancel();
        FrequencyRangeText.Text = "N/A";
        confirmedState = null;
        hasUnstoredChanges = false;
        DevicePanel.IsEnabled = false;
        ApplyButton.IsEnabled = false;
        StoreButton.IsEnabled = false;
        ConnectionIndicator.Fill = Brushes.Red;
        RfIndicator.IsRfOn = null;
        LockIndicator.Fill = Brushes.Gray;
        ConnectionStateText.Text = "Disconnected";
    }
    private void RefreshPorts(string? preferred = null)
    {
        SetConnectionDetails();
        var previous = PortSelector.SelectedItem as string; var ports = portProbeService.GetAvailablePorts(); isRefreshingPorts = true;
        var selection = previous ?? preferred ?? connectionProfileService.Load()?.PortName;
        try { PortSelector.ItemsSource = ports; PortSelector.SelectedItem = ports.FirstOrDefault(port => string.Equals(port, selection, StringComparison.OrdinalIgnoreCase)); }
        finally { isRefreshingPorts = false; }
        StatusMessage.Text = ports.Count == 0 ? "No serial ports were found. Connect the G6 serial interface, then refresh."
            : PortSelector.SelectedItem is string selected ? $"{selected} selected. Click Connect to read the G6."
            : selection is not null ? $"Remembered port {selection} is unavailable. Select a port, then click Connect."
            : "Select a serial port, then click Connect.";
        UpdateConnectionControls();
    }
    private static Brush StatusBrush(bool active) => active ? Brushes.SeaGreen : Brushes.Gray;

    private void UpdateActionButtons()
    {
        G6DeviceState? editedState = null;
        var hasValidState = portProbeService.IsConnected && TryBuildState(out editedState, out _);
        var hasPendingEdits = hasValidState && editedState != confirmedState;
        ApplyButton.IsEnabled = hasPendingEdits;
        StoreButton.IsEnabled = hasValidState && !hasPendingEdits && hasUnstoredChanges;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetSystemMenu(IntPtr windowHandle, bool revert);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr menuHandle, uint flags, int itemId, string? itemText);

    [DllImport("user32.dll")]
    private static extern uint CheckMenuItem(IntPtr menuHandle, int itemId, uint checkFlags);
}
