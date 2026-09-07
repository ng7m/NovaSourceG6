using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public sealed class SerialPortProbeServiceTests
{
    [Fact]
    public void GetAvailablePorts_SortsAndRemovesBlankNames()
    {
        var service = CreateService(new FakePortProvider("COM12", "", "COM3"));
        Assert.Equal(["COM3", "COM12"], service.GetAvailablePorts());
    }

    [Fact]
    public void GetAvailablePorts_UsesNaturalNumericOrderAndPlacesOtherNamesLast()
    {
        var service = CreateService(new FakePortProvider("COM50", "VIRTUAL", "COM2", "COM10", "COM1"));
        Assert.Equal(["COM1", "COM2", "COM10", "COM50", "VIRTUAL"], service.GetAvailablePorts());
    }

    [Fact]
    public async Task ProbeAsync_WithoutASelection_ReturnsActionableFailure()
    {
        var result = await CreateService(new FakePortProvider("COM3")).ProbeAsync(null);
        Assert.False(result.Succeeded);
        Assert.Equal("Select a serial port before testing.", result.Message);
    }

    [Fact]
    public async Task ProbeAsync_ForAvailablePort_SendsOnlyCarriageReturnAndAcceptsFragmentedPrompt()
    {
        var provider = new FakePortProvider("COM50");
        provider.Connection.Responses.Enqueue("OK ");
        provider.Connection.Responses.Enqueue("\r> ");

        var service = CreateService(provider);
        var result = await service.ProbeAsync("COM50");

        Assert.True(result.Succeeded);
        Assert.Equal("\r", provider.Connection.WrittenText);
        Assert.True(provider.Connection.BufferDiscarded);
        Assert.False(provider.Connection.Disposed);
        Assert.Equal("OK \r>", result.RawResponse);

        service.Disconnect();

        Assert.True(provider.Connection.Disposed);
        Assert.False(service.IsConnected);
    }

    [Theory]
    [InlineData("OK\r>")]
    [InlineData("OK \r> ")]
    [InlineData("NS G6 v1.0\r(c) 2002 Nova Engineering, Inc.\r>")]
    public void IsNovaSourcePrompt_AcceptsDocumentedResponseForms(string response) =>
        Assert.True(SerialPortProbeService.IsNovaSourcePrompt(response));

    [Theory]
    [InlineData("")]
    [InlineData("OK")]
    [InlineData("ER 1\r>")]
    [InlineData("OTHER\r>")]
    [InlineData("OK\r>unexpected")]
    public void IsNovaSourcePrompt_RejectsUnexpectedResponses(string response) =>
        Assert.False(SerialPortProbeService.IsNovaSourcePrompt(response));

    [Fact]
    public void FormatResponse_ShowsOnlyPrintableAsciiCharacters()
    {
        Assert.Equal("OK > ", SerialPortProbeResult.FormatResponse("OK \r> \n\u0001"));
    }

    [Fact]
    public void ParseCommandResponse_ExtractsQueryValue()
    {
        var result = SerialPortProbeService.ParseCommandResponse("FR", "OK 2400.000\r> ");
        Assert.True(result.Succeeded);
        Assert.Equal("2400.000", result.Value);
    }

    [Theory]
    [InlineData("ER 1\r>", "unknown command")]
    [InlineData("ER 2\r>", "value outside the legal range")]
    [InlineData("ER 3\r>", "device command failure")]
    public void ParseCommandResponse_ExplainsDeviceErrors(string response, string expected)
    {
        var result = SerialPortProbeService.ParseCommandResponse("FR", response);
        Assert.False(result.Succeeded);
        Assert.Contains(expected, result.Message);
    }

    [Fact]
    public async Task ProbeAsync_WhenNoResponseArrives_ReturnsTimeout()
    {
        var result = await CreateService(new FakePortProvider("COM50")).ProbeAsync("COM50");
        Assert.False(result.Succeeded);
        Assert.Contains("after 3 attempts", result.Message);
        Assert.Contains("Attempt 3:", result.RawResponse);
        Assert.Contains("didn’t respond", result.UserMessage);
    }

    [Fact]
    public async Task ProbeAsync_AfterSuccess_ExposesActivePortUntilDisconnected()
    {
        var provider = new FakePortProvider("COM50");
        provider.Connection.Responses.Enqueue("OK \r> ");
        var service = CreateService(provider);

        await service.ProbeAsync("COM50");

        Assert.True(service.IsConnected);
        Assert.Equal("COM50", service.ActivePortName);

        service.Disconnect();

        Assert.False(service.IsConnected);
        Assert.Null(service.ActivePortName);
    }

    [Fact]
    public async Task ProbeAsync_ForPortInUse_ReturnsActionableFailure()
    {
        var provider = new FakePortProvider("COM50") { OpenException = new UnauthorizedAccessException() };
        var result = await CreateService(provider).ProbeAsync("COM50");
        Assert.False(result.Succeeded);
        Assert.StartsWith("COM50 is already in use or access is denied.", result.Message);
    }

    [Fact]
    public async Task ProbeAsync_WhenWriteTimesOut_ReturnsFailureAndDisposesConnection()
    {
        var provider = new FakePortProvider("COM51") { WriteException = new TimeoutException() };

        var result = await CreateService(provider).ProbeAsync("COM51");

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Timed out while sending the validation request to COM51.",
            result.Message);
        Assert.True(provider.Connection.Disposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("OK 1000.000\r")]
    public async Task CommandTimeoutDisconnectsAndPreventsSubsequentWrites(string partial)
    {
        var provider = new FakePortProvider("COM50");
        provider.Connection.Responses.Enqueue("OK\r>");
        using var service = CreateService(provider);
        Assert.True((await service.ProbeAsync("COM50")).Succeeded);
        provider.Connection.Responses.Enqueue(partial);
        Assert.False((await service.ExecuteCommandAsync("FR 1000.000")).Succeeded);
        Assert.False(service.IsConnected);
        Assert.True(provider.Connection.Disposed);
        Assert.False((await service.ExecuteCommandAsync("AT 5")).Succeeded);
        Assert.Equal("\rFR 1000.000\r", provider.Connection.WrittenText);
    }

    [Fact]
    public async Task TransportFailureDisconnects()
    {
        var provider = new FakePortProvider("COM50");
        provider.Connection.Responses.Enqueue("OK\r>");
        using var service = CreateService(provider);
        await service.ProbeAsync("COM50");
        provider.Connection.WriteException = new System.IO.IOException("Cable removed");
        Assert.False((await service.ExecuteCommandAsync("FR 1000.000")).Succeeded);
        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task CancellationAfterWriteDiscardsConnectionWithPotentialLateResponse()
    {
        var provider = new FakePortProvider("COM50");
        provider.Connection.Responses.Enqueue("OK\r>");
        using var service = new SerialPortProbeService(provider, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1));
        await service.ProbeAsync("COM50");
        using var cancellation = new CancellationTokenSource();
        var task = service.ExecuteCommandAsync("FR 1000.000", cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(service.IsConnected);
        Assert.Equal("\rFR 1000.000\r", provider.Connection.WrittenText);
    }

    private static SerialPortProbeService CreateService(FakePortProvider provider) =>
        new(provider, TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(1));

    private sealed class FakePortProvider(params string[] ports) : ISerialPortProvider
    {
        public FakeConnection Connection { get; } = new();
        public Exception? OpenException { get; init; }
        public Exception? WriteException { get; init; }
        public IReadOnlyList<string> GetPortNames() => ports;

        public ISerialPortConnection CreateConnection(string portName)
        {
            Connection.ExceptionToThrow = OpenException;
            Connection.WriteException = WriteException;
            return Connection;
        }
    }

    private sealed class FakeConnection : ISerialPortConnection
    {
        public Queue<string> Responses { get; } = new();
        private readonly Queue<string> pendingReads = new();
        public bool BufferDiscarded { get; private set; }
        public bool Disposed { get; private set; }
        public string WrittenText { get; private set; } = string.Empty;
        public Exception? ExceptionToThrow { get; set; }
        public Exception? WriteException { get; set; }

        public void Open()
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
        }

        public void DiscardInBuffer() => BufferDiscarded = true;
        public void Write(string value)
        {
            if (WriteException is not null) throw WriteException;
            WrittenText += value;
            while (Responses.TryDequeue(out var response)) pendingReads.Enqueue(response);
        }
        public string ReadExisting() => pendingReads.TryDequeue(out var response) ? response : string.Empty;
        public void Dispose() => Disposed = true;
    }
}
