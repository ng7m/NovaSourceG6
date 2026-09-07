using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public class WindowPlacementPolicyTests
{
    private static readonly ScreenArea Primary = new("primary", 0, 0, 1920, 1040, 96, true);
    private static readonly ScreenArea Secondary = new("secondary", -2560, 0, 2560, 1400, 144, false);

    [Fact]
    public void RestoresNegativeCoordinatesOnSecondaryMonitor()
    {
        var saved = new SavedWindowPlacement("secondary", -2200, 100, 1050, 1200, 144, true);
        Assert.Equal(saved, WindowPlacementPolicy.Resolve(saved, [Primary, Secondary]));
    }

    [Fact]
    public void RemovedMonitorFallsBackToPrimaryAndRescalesSize()
    {
        var result = WindowPlacementPolicy.Resolve(new("secondary", -2200, 100, 1050, 1200, 144, false), [Primary]);
        Assert.Equal("primary", result.Device);
        Assert.Equal(700, result.Width);
        Assert.Equal(800, result.Height);
        Assert.InRange(result.Left, 0, 1220);
        Assert.InRange(result.Top, 0, 240);
    }

    [Fact]
    public void ChangedDpiPreservesLogicalSize()
    {
        var result = WindowPlacementPolicy.Resolve(new("primary", 50, 50, 700, 800, 96, false), [Primary with { Width = 3840, Height = 2080, Dpi = 192 }]);
        Assert.Equal(1400, result.Width);
        Assert.Equal(1600, result.Height);
    }

    [Fact]
    public void OversizedPlacementFitsWorkingAreaIncludingTaskbarOffset()
    {
        var screen = Primary with { Left = 60, Top = 40, Width = 740, Height = 560 };
        var result = WindowPlacementPolicy.Resolve(new("primary", -10, -10, 2000, 2000, 96, false), [screen]);
        Assert.Equal(60, result.Left);
        Assert.Equal(40, result.Top);
        Assert.Equal(740, result.Width);
        Assert.Equal(560, result.Height);
    }

    [Fact]
    public void FullyOffScreenPlacementReturnsToPrimaryEvenWhenNamedMonitorExists()
    {
        var result = WindowPlacementPolicy.Resolve(new("secondary", 9000, 9000, 700, 800, 96, false), [Primary, Secondary]);
        Assert.Equal("primary", result.Device);
        Assert.InRange(result.Left, 0, Primary.Width - result.Width);
        Assert.InRange(result.Top, 0, Primary.Height - result.Height);
    }

    [Fact]
    public void CorruptPlacementUsesDefaults()
    {
        var result = WindowPlacementPolicy.Resolve(new("primary", int.MaxValue, 0, -100, 800, 0, true), [Primary]);
        Assert.Equal(520, result.Width);
        Assert.Equal(690, result.Height);
        Assert.False(result.Maximized);
    }

    [Fact]
    public void TinyWorkingAreaDoesNotThrowOrPlaceControlsOutsideWindowBounds()
    {
        var result = WindowPlacementPolicy.Resolve(null, [Primary with { Width = 280, Height = 200 }]);
        Assert.Equal(280, result.Width);
        Assert.Equal(200, result.Height);
    }
}
