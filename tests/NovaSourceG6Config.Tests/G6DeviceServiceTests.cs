using System.IO;
using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public sealed class G6DeviceServiceTests
{
    [Fact]
    public async Task ReadStateAsync_UsesDocumentedQueryOrderAndMapsDeviceState()
    {
        using var fixture = await ConnectedFixture.CreateAsync(
            Query("LF", "OK 950.000\r> "),
            Query("HF", "OK 2150.000\r> "),
            Query("FR", "OK 1450.125\r> "),
            Query("AT", "OK 7\r> "),
            Query("IM", "OK M\r> "),
            Query("TM", "OK C\r> "),
            Query("IT", "OK E\r> "),
            Query("RS", "OK D\r> "),
            Query("MS", "OK I\r> "),
            Query("MG", "OK -2\r> "),
            Query("LS", "OK 7\r> "));

        var state = await fixture.Device.ReadStateAsync();

        Assert.Equal(1450.125m, state.FrequencyMhz);
        Assert.Equal(950.000m, state.MinimumFrequencyMhz);
        Assert.Equal(2150.000m, state.MaximumFrequencyMhz);
        Assert.Equal(7, state.Attenuation);
        Assert.Equal("M", state.InputMode);
        Assert.Equal("C", state.TriggerMode);
        Assert.True(state.InternalTriggerEnabled);
        Assert.False(state.RfStandbyEnabled);
        Assert.Equal("I", state.ModulationSource);
        Assert.Equal(-2, state.ModulationGain);
        Assert.True(state.RfOn);
        Assert.True(state.Locked);
        Assert.True(state.PowerOn);
        Assert.Equal(
            ["", "LF", "HF", "FR", "AT", "IM", "TM", "IT", "RS", "MS", "MG", "LS"],
            fixture.Simulator.Commands);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task ReadStateAsync_AcceptsFragmentedTranscript()
    {
        using var fixture = await ConnectedFixture.CreateAsync(
            Query("LF", "OK ", "950.000\r", "> "),
            Query("HF", "OK 2150.000\r> "),
            Query("FR", "OK 1450.125\r> "),
            Query("AT", "OK 7\r> "),
            Query("IM", "OK M\r> "),
            Query("TM", "OK C\r> "),
            Query("IT", "OK E\r> "),
            Query("RS", "OK D\r> "),
            Query("MS", "OK I\r> "),
            Query("MG", "OK -2\r> "),
            Query("LS", "OK 3\r> "));

        var state = await fixture.Device.ReadStateAsync();

        Assert.False(state.RfOn);
        Assert.True(state.Locked);
        Assert.Contains("RX <OK >", fixture.Simulator.Transcript);
        Assert.Contains("RX <950.000\\r>", fixture.Simulator.Transcript);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task ReadStateAsync_RejectsUnknownLedStatus()
    {
        using var fixture = await ConnectedFixture.CreateAsync(
            Query("LF", "OK 950.000\r> "), Query("HF", "OK 2150.000\r> "),
            Query("FR", "OK 1450.125\r> "), Query("AT", "OK 7\r> "),
            Query("IM", "OK M\r> "), Query("TM", "OK C\r> "),
            Query("IT", "OK E\r> "), Query("RS", "OK D\r> "),
            Query("MS", "OK I\r> "), Query("MG", "OK -2\r> "),
            Query("LS", "OK 9\r> "));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => fixture.Device.ReadStateAsync());

        Assert.Contains("unknown LED status value: 9", exception.Message);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task ApplyChangesAsync_UsesSafeCommandOrderAndCanonicalValues()
    {
        using var fixture = await ConnectedFixture.CreateAsync(
            Set("FR 1500.500"), Set("AT 12"), Set("MS N"), Set("IM T"),
            Set("TM T"), Set("RS E"), Set("MG 3"));
        var original = State(modulationSource: "E", inputMode: "M");
        var updated = original with
        {
            FrequencyMhz = 1500.5m,
            Attenuation = 12,
            ModulationSource = "N",
            InputMode = "T",
            TriggerMode = "T",
            RfStandbyEnabled = true,
            ModulationGain = 3
        };

        var applied = await fixture.Device.ApplyChangesAsync(original, updated);

        Assert.Equal(7, applied);
        Assert.Equal(
            ["", "FR 1500.500", "AT 12", "MS N", "IM T", "TM T", "RS E", "MG 3"],
            fixture.Simulator.Commands);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task ApplyChangesAsync_StopsAfterFirstRejectedCommandAndRetainsTranscript()
    {
        using var fixture = await ConnectedFixture.CreateAsync(
            Set("FR 1500.500"),
            new("AT 12", ["ER 2\r> "]));
        var original = State();
        var updated = original with { FrequencyMhz = 1500.5m, Attenuation = 12, TriggerMode = "T" };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Device.ApplyChangesAsync(original, updated));

        Assert.Contains("value outside the legal range", exception.Message);
        Assert.Equal(["", "FR 1500.500", "AT 12"], fixture.Simulator.Commands);
        Assert.DoesNotContain("TM T", fixture.Simulator.Commands);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task ApplyChangesAsync_ChangesInternalTriggerOnlyForInternalInputMode()
    {
        using var fixture = await ConnectedFixture.CreateAsync(Set("IT E"));
        var original = State();

        var applied = await fixture.Device.ApplyChangesAsync(
            original, original with { InternalTriggerEnabled = true });

        Assert.Equal(1, applied);
        Assert.Equal(["", "IT E"], fixture.Simulator.Commands);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task LoadAndStore_EmitDocumentedActionCommands()
    {
        using var fixture = await ConnectedFixture.CreateAsync(Set("LD"), Set("ST"));

        await fixture.Device.LoadAsync();
        await fixture.Device.StoreAsync();

        Assert.Equal(["", "LD", "ST"], fixture.Simulator.Commands);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Fact]
    public async Task ReadStateAsync_ReportsAnIncompleteResponse()
    {
        using var fixture = await ConnectedFixture.CreateAsync(Query("LF", "OK 950.000\r"));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => fixture.Device.ReadStateAsync());

        Assert.Contains("incomplete response to LF", exception.Message);
        Assert.Equal(["", "LF"], fixture.Simulator.Commands);
        fixture.Simulator.AssertScriptConsumed();
    }

    [Theory]
    [InlineData("ER 1\r> ", "unknown command")]
    [InlineData("ER 2\r> ", "value outside the legal range")]
    [InlineData("ER 3\r> ", "device command failure")]
    public async Task SetFrequencyAsync_PropagatesDeviceErrors(string response, string expectedMessage)
    {
        using var fixture = await ConnectedFixture.CreateAsync(
            new ScriptedCommand("FR 1000.000", [response]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Device.SetFrequencyAsync(1000m));

        Assert.Contains(expectedMessage, exception.Message);
        fixture.Simulator.AssertScriptConsumed();
    }

    private static ScriptedCommand Query(string command, params string[] responseFragments) =>
        new(command, responseFragments);

    private static ScriptedCommand Set(string command) => new(command, ["OK\r> "]);

    private static G6DeviceState State(string modulationSource = "I", string inputMode = "M") =>
        new(1450m, 950m, 2150m, 5, inputMode, "C", false, false,
            modulationSource, 0, false, true, true);

    private sealed class ConnectedFixture : IDisposable
    {
        private readonly SerialPortProbeService serial;

        public G6ProtocolSimulator Simulator { get; }
        public G6DeviceService Device { get; }

        private ConnectedFixture(G6ProtocolSimulator simulator, SerialPortProbeService serial)
        {
            Simulator = simulator;
            this.serial = serial;
            Device = new(serial);
        }

        public static async Task<ConnectedFixture> CreateAsync(params ScriptedCommand[] commands)
        {
            var simulator = new G6ProtocolSimulator(commands);
            var serial = new SerialPortProbeService(
                simulator, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1));
            var result = await serial.ProbeAsync("COM50");
            Assert.True(result.Succeeded, result.Message);
            return new(simulator, serial);
        }

        public void Dispose() => serial.Dispose();
    }

    private sealed record ScriptedCommand(string Command, IReadOnlyList<string> ResponseFragments);

    private sealed class G6ProtocolSimulator(IEnumerable<ScriptedCommand> commands) : ISerialPortProvider
    {
        private readonly Queue<ScriptedCommand> script = new(commands);
        private readonly Queue<string> pendingReads = new();

        public List<string> Commands { get; } = [];
        public List<string> Transcript { get; } = [];

        public IReadOnlyList<string> GetPortNames() => ["COM50"];

        public ISerialPortConnection CreateConnection(string portName)
        {
            Assert.Equal("COM50", portName);
            return new SimulatedConnection(this);
        }

        public void AssertScriptConsumed() => Assert.Empty(script);

        private void Write(string value)
        {
            var command = value.TrimEnd('\r');
            Commands.Add(command);
            Transcript.Add($"TX <{Escape(value)}>");
            if (command.Length == 0)
            {
                pendingReads.Enqueue("OK \r> ");
                return;
            }

            Assert.NotEmpty(script);
            var step = script.Dequeue();
            Assert.Equal(step.Command, command);
            foreach (var fragment in step.ResponseFragments) pendingReads.Enqueue(fragment);
        }

        private string Read()
        {
            var value = pendingReads.TryDequeue(out var fragment) ? fragment : string.Empty;
            if (value.Length > 0) Transcript.Add($"RX <{Escape(value)}>");
            return value;
        }

        private static string Escape(string value) =>
            value.Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);

        private sealed class SimulatedConnection(G6ProtocolSimulator owner) : ISerialPortConnection
        {
            public void Open() { }
            public void DiscardInBuffer() => owner.pendingReads.Clear();
            public void Write(string value) => owner.Write(value);
            public string ReadExisting() => owner.Read();
            public void Dispose() { }
        }
    }
}
