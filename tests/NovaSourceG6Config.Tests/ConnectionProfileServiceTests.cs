using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public sealed class ConnectionProfileServiceTests
{
    [Fact]
    public void Save_WithValidValues_PersistsProfileAndBuildsFriendlyLabel()
    {
        var store = new FakeProfileStore();
        var result = new ConnectionProfileService(store).Save("Remote lab G6", "COM50", "115200");
        Assert.True(result.Succeeded);
        Assert.Equal("Remote lab G6 (COM50)", result.Profile?.DisplayName);
        Assert.Equal(result.Profile, store.Profile);
    }

    [Fact]
    public void Load_AfterSuccessfulSave_ReturnsRememberedPort()
    {
        var store = new FakeProfileStore();
        var service = new ConnectionProfileService(store);

        var saveResult = service.Save(string.Empty, "COM50", "38400");
        var loadedProfile = service.Load();

        Assert.True(saveResult.Succeeded);
        Assert.Equal("COM50", loadedProfile?.PortName);
        Assert.Equal(38400, loadedProfile?.BaudRate);
    }

    [Fact]
    public void Save_WithoutPort_ReturnsActionableFailure()
    {
        var result = new ConnectionProfileService(new FakeProfileStore()).Save("Lab G6", null, "115200");
        Assert.False(result.Succeeded);
        Assert.Equal("Select a serial port before saving the connection profile.", result.Message);
    }

    [Fact]
    public void Save_WithUnsupportedBaudRate_ReturnsActionableFailure()
    {
        var result = new ConnectionProfileService(new FakeProfileStore()).Save("Lab G6", "COM3", "12345");
        Assert.False(result.Succeeded);
        Assert.Equal("Select a supported baud rate.", result.Message);
    }

    private sealed class FakeProfileStore : IConnectionProfileStore
    {
        public ConnectionProfile? Profile { get; private set; }
        public ConnectionProfile? Load() => Profile;
        public void Save(ConnectionProfile profile) => Profile = profile;
    }
}
