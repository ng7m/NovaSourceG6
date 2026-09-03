using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public sealed class SerialPortProbeServiceTests
{
    [Fact]
    public void GetAvailablePorts_SortsAndRemovesBlankNames()
    {
        var service = new SerialPortProbeService(new FakePortProvider("COM12", "", "COM3"));

        Assert.Equal(["COM12", "COM3"], service.GetAvailablePorts());
    }

    [Fact]
    public void Probe_WithoutASelection_ReturnsActionableFailure()
    {
        var service = new SerialPortProbeService(new FakePortProvider("COM3"));

        var result = service.Probe(null);

        Assert.False(result.Succeeded);
        Assert.Equal("Select a serial port before testing.", result.Message);
    }

    [Fact]
    public void Probe_ForAvailablePort_OpensThenDisposesConnectionWithoutSendingData()
    {
        var provider = new FakePortProvider("COM50");
        var service = new SerialPortProbeService(provider);

        var result = service.Probe("COM50");

        Assert.True(result.Succeeded);
        Assert.True(provider.Connection.Opened);
        Assert.True(provider.Connection.Disposed);
    }

    [Fact]
    public void Probe_ForPortInUse_ReturnsActionableFailure()
    {
        var provider = new FakePortProvider("COM50") { OpenException = new UnauthorizedAccessException() };
        var service = new SerialPortProbeService(provider);

        var result = service.Probe("COM50");

        Assert.False(result.Succeeded);
        Assert.Equal("COM50 is already in use or access is denied.", result.Message);
    }

    private sealed class FakePortProvider(params string[] ports) : ISerialPortProvider
    {
        public FakeConnection Connection { get; } = new();
        public Exception? OpenException { get; init; }

        public IReadOnlyList<string> GetPortNames() => ports;

        public ISerialPortConnection CreateConnection(string portName)
        {
            Connection.ExceptionToThrow = OpenException;
            return Connection;
        }
    }

    private sealed class FakeConnection : ISerialPortConnection
    {
        public bool Opened { get; private set; }
        public bool Disposed { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public void Open()
        {
            Opened = true;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
        }

        public void Dispose() => Disposed = true;
    }
}
