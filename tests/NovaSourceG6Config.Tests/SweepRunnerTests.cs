using NovaSourceG6Config;
using System.IO;

namespace NovaSourceG6Config.Tests;

public class SweepRunnerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RespectsDirectionBoundsAndRepeatCount(bool ascending)
    {
        var values = new List<decimal>();
        var runner = new SweepRunner((value, _) => { values.Add(value); return Task.CompletedTask; });
        await runner.RunAsync(1000, 1001, 0.5m, 50, 2, ascending, (_, _, _, _) => { }, CancellationToken.None);
        decimal[] pass = ascending ? [1000, 1000.5m, 1001] : [1001, 1000.5m, 1000];
        Assert.Equal(pass.Concat(pass), values);
    }

    [Fact]
    public async Task PausePreventsNextCommandAndResumeCompletesSweep()
    {
        var values = new List<decimal>();
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new SweepRunner((value, _) => { values.Add(value); return Task.CompletedTask; });
        var task = runner.RunAsync(1000, 1001, 1, 50, 1, true, (_, index, _, _) =>
        {
            if (index == 1) { runner.Pause(); paused.SetResult(); }
        }, CancellationToken.None);
        await paused.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(120);
        Assert.Single(values);
        runner.Resume();
        await task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal([1000m, 1001m], values);
    }

    [Fact]
    public async Task StopWhilePausedPreventsAllFurtherCommands()
    {
        var calls = 0;
        var runner = new SweepRunner((_, _) => { calls++; return Task.CompletedTask; });
        using var cancellation = new CancellationTokenSource();
        runner.Pause();
        var task = runner.RunAsync(1000, 1001, 1, 50, 0, true, (_, _, _, _) => { }, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(2)));
        runner.Resume();
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task StopDuringInFlightCommandDoesNotStartAnotherCommand()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var cancellation = new CancellationTokenSource();
        var runner = new SweepRunner(async (_, token) =>
        {
            calls++;
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
        });
        var task = runner.RunAsync(1000, 1001, 1, 50, 0, true, (_, _, _, _) => { }, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CommandFailureStopsSweepImmediately()
    {
        var calls = 0;
        var runner = new SweepRunner((_, _) => { calls++; throw new IOException("transport lost"); });
        await Assert.ThrowsAsync<IOException>(() => runner.RunAsync(1000, 1001, 1, 50, 0, true, (_, _, _, _) => { }, CancellationToken.None));
        Assert.Equal(1, calls);
    }
}
