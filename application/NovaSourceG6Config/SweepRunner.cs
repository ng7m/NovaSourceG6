namespace NovaSourceG6Config;

public sealed class SweepRunner(Func<decimal, CancellationToken, Task> setFrequency)
{
    private TaskCompletionSource? resume;
    public bool IsPaused => resume is not null;
    public void Pause() => resume ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Resume() { var pending = resume; resume = null; pending?.TrySetResult(); }

    public async Task RunAsync(decimal low, decimal high, decimal step, int dwell, int repeats, bool ascending,
        Action<decimal, int, int, long> progress, CancellationToken token)
    {
        if (low >= high || step < 0.001m || dwell is < 50 or > 60000 || repeats < 0)
            throw new ArgumentOutOfRangeException(nameof(step), "Invalid sweep settings.");
        var points = checked((int)decimal.Floor((high - low) / step) + 1);
        for (long pass = 1; repeats == 0 || pass <= repeats; pass++)
        {
            for (var index = 0; index < points; index++)
            {
                token.ThrowIfCancellationRequested();
                while (resume is { } pending) await pending.Task.WaitAsync(token);
                token.ThrowIfCancellationRequested();
                var frequency = ascending ? low + index * step : high - index * step;
                await setFrequency(frequency, token);
                progress(frequency, index + 1, points, pass);
                await Task.Delay(dwell, token);
            }
        }
    }
}
