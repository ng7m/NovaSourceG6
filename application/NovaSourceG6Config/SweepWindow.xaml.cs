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
    private readonly ManualResetEventSlim pauseGate = new(true);
    private bool paused;
    private bool closeWhenSweepStops;

    public decimal? AppliedFrequencyMhz { get; private set; }

    public SweepWindow(G6DeviceService device, decimal minimum, decimal maximum, decimal current)
    {
        InitializeComponent(); this.device = device; this.minimum = minimum; this.maximum = maximum;
        StartTextBox.Text = current.ToString("0.000", CultureInfo.InvariantCulture); StopTextBox.Text = maximum.ToString("0.000", CultureInfo.InvariantCulture);
        EstimateText.Text = $"Allowed range: {minimum:0.000}–{maximum:0.000} MHz. Dwell: 50–60,000 ms.";
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadSettings(out var start, out var stop, out var step, out var dwell, out var repeats, out var error)) { SweepStatus.Text = error; return; }
        sweepCancellation = new(); paused = false; pauseGate.Set(); SetRunning(true); var token = sweepCancellation.Token;
        try
        {
            var ascending = DirectionSelector.SelectedIndex == 0; var low = Math.Min(start, stop); var high = Math.Max(start, stop);
            var points = (int)Math.Floor((high - low) / step) + 1; var pass = 0;
            var secondsPerPass = points * dwell / 1000d;
            EstimateText.Text = repeats == 0
                ? $"{points:N0} points per pass · {TimeSpan.FromSeconds(secondsPerPass):g} per pass · continuous"
                : $"{points:N0} points per pass · estimated {TimeSpan.FromSeconds(secondsPerPass * repeats):g}";
            while (repeats == 0 || pass < repeats)
            {
                pass++;
                for (var index = 0; index < points; index++)
                {
                    token.ThrowIfCancellationRequested(); await Task.Run(() => pauseGate.Wait(token), token);
                    var frequency = ascending ? low + index * step : high - index * step;
                    await device.SetFrequencyAsync(frequency, token);
                    AppliedFrequencyMhz = frequency;
                    SweepProgress.Value = (index + 1d) / points * 100d;
                    SweepStatus.Text = $"Pass {pass}{(repeats == 0 ? "" : $" of {repeats}")} · {frequency:0.000} MHz";
                    await Task.Delay(dwell, token);
                }
            }
            SweepStatus.Text = "Sweep complete.";
        }
        catch (OperationCanceledException) { SweepStatus.Text = "Sweep stopped."; }
        catch (Exception exception) { SweepStatus.Text = $"Sweep stopped because a device command failed: {exception.Message}"; }
        finally
        {
            sweepCancellation?.Dispose();
            sweepCancellation = null;
            pauseGate.Set();
            SetRunning(false);
            if (closeWhenSweepStops) _ = Dispatcher.BeginInvoke(new Action(Close));
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e) { paused = !paused; if (paused) pauseGate.Reset(); else pauseGate.Set(); PauseButton.Content = paused ? "Resume" : "Pause"; SweepStatus.Text = paused ? "Sweep paused." : "Sweep resumed."; }
    private void Stop_Click(object sender, RoutedEventArgs e) { pauseGate.Set(); sweepCancellation?.Cancel(); }
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (sweepCancellation is null) return;
        e.Cancel = true;
        closeWhenSweepStops = true;
        pauseGate.Set();
        sweepCancellation.Cancel();
    }
    private void SetRunning(bool running) { StartButton.IsEnabled = !running; PauseButton.IsEnabled = running; StopButton.IsEnabled = running; StartTextBox.IsEnabled = !running; StopTextBox.IsEnabled = !running; RepeatTextBox.IsEnabled = !running; }

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
