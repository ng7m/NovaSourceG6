using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace NovaSourceG6Config;

public partial class SweepWindow : Window
{
    private readonly G6DeviceService device;
    private readonly decimal minimum;
    private readonly decimal maximum;
    private CancellationTokenSource? sweepCancellation;
    private readonly SweepRunner runner;
    private bool closeWhenSweepStops;

    public decimal? AppliedFrequencyMhz { get; private set; }
    public bool StateUncertain { get; private set; }

    public SweepWindow(G6DeviceService device, decimal minimum, decimal maximum, decimal current)
    {
        InitializeComponent(); this.device = device; this.minimum = minimum; this.maximum = maximum;
        DialogPlacement.Configure(this);
        runner = new(device.SetFrequencyAsync);
        StartTextBox.Text = current.ToString("0.000", CultureInfo.InvariantCulture); StopTextBox.Text = maximum.ToString("0.000", CultureInfo.InvariantCulture);
        EstimateText.Text = $"Allowed range: {minimum:0.000}–{maximum:0.000} MHz. Dwell: 50–60,000 ms.";
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadSettings(out var start, out var stop, out var step, out var dwell, out var repeats, out var error)) { SweepStatus.Text = error; return; }
        sweepCancellation = new(); runner.Resume(); SetRunning(true); var token = sweepCancellation.Token;
        try
        {
            var ascending = DirectionSelector.SelectedIndex == 0; var low = Math.Min(start, stop); var high = Math.Max(start, stop);
            var points = checked((int)Math.Floor((high - low) / step) + 1);
            var secondsPerPass = points * dwell / 1000d;
            EstimateText.Text = repeats == 0
                ? $"{points:N0} points per pass · {TimeSpan.FromSeconds(secondsPerPass):g} per pass · continuous"
                : $"{points:N0} points per pass · {repeats:N0} passes · {secondsPerPass * repeats:N0} seconds plus command time";
            await runner.RunAsync(low, high, step, dwell, repeats, ascending, (frequency, completed, total, pass) =>
            {
                    AppliedFrequencyMhz = frequency;
                    SweepProgress.Value = completed / (double)total * 100d;
                    SweepStatus.Text = $"Pass {pass}{(repeats == 0 ? "" : $" of {repeats}")} · {frequency:0.000} MHz";
            }, token);
            SweepStatus.Text = "Sweep complete.";
        }
        catch (OperationCanceledException) { SweepStatus.Text = "Sweep stopped. Close this window to verify the final frequency."; }
        catch (Exception exception) { StateUncertain = true; SweepStatus.Text = $"Sweep stopped; device state is unverified: {exception.Message}. Close this window to reconnect."; }
        finally
        {
            sweepCancellation?.Dispose();
            sweepCancellation = null;
            runner.Resume();
            SetRunning(false);
            if (closeWhenSweepStops) _ = Dispatcher.BeginInvoke(new Action(Close));
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e) { if (runner.IsPaused) runner.Resume(); else runner.Pause(); PauseButton.Content = runner.IsPaused ? "Resume" : "Pause"; SweepStatus.Text = runner.IsPaused ? "Pause requested; any in-flight command may finish." : "Sweep resumed."; }
    private void Stop_Click(object sender, RoutedEventArgs e) { sweepCancellation?.Cancel(); runner.Resume(); }
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (sweepCancellation is null) return;
        e.Cancel = true;
        closeWhenSweepStops = true;
        sweepCancellation.Cancel();
        runner.Resume();
    }
    private void SetRunning(bool running) { StartButton.IsEnabled = !running && !StateUncertain; PauseButton.IsEnabled = running; PauseButton.Content = "Pause"; StopButton.IsEnabled = running; StartTextBox.IsEnabled = !running; StopTextBox.IsEnabled = !running; RepeatTextBox.IsEnabled = !running; StepTextBox.IsEnabled = !running; DwellTextBox.IsEnabled = !running; DirectionSelector.IsEnabled = !running; }

    private bool TryReadSettings(out decimal start, out decimal stop, out decimal step, out int dwell, out int repeats, out string error)
    {
        error = string.Empty;
        if (!decimal.TryParse(StartTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out start) || start < minimum || start > maximum) { error = $"Start frequency must be within {minimum:0.000}–{maximum:0.000} MHz."; stop = step = 0; dwell = repeats = 0; return false; }
        if (!decimal.TryParse(StopTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out stop) || stop < minimum || stop > maximum || stop == start) { error = "Stop frequency must be within the device range and different from start."; step = 0; dwell = repeats = 0; return false; }
        if (!decimal.TryParse(StepTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out step) || step < 0.001m || step > maximum - minimum) { error = "Step must be at least 0.001 MHz and no larger than the device range."; dwell = repeats = 0; return false; }
        if (!int.TryParse(DwellTextBox.Text, out dwell) || dwell is < 50 or > 60000) { error = "Dwell must be from 50 through 60,000 milliseconds."; repeats = 0; return false; }
        if (!int.TryParse(RepeatTextBox.Text, out repeats) || repeats < 0) { error = "Repeat count must be zero or a positive whole number."; return false; }
        return true;
    }
}
