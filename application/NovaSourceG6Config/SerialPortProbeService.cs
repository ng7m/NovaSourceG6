using System.IO;

namespace NovaSourceG6Config;

public interface ISerialPortProvider
{
    IReadOnlyList<string> GetPortNames();
    ISerialPortConnection CreateConnection(string portName);
}

public interface ISerialPortConnection : IDisposable
{
    void Open();
    void DiscardInBuffer();
    void Write(string value);
    string ReadExisting();
}

public sealed record SerialPortProbeResult(bool Succeeded, string Message, string RawResponse = "")
{
    public string? UserMessage { get; init; }
    public static SerialPortProbeResult Success(string portName, string response) =>
        new(true, $"{portName} returned the NovaSource G6 command prompt: {FormatResponse(response)}", response);

    public static SerialPortProbeResult Failure(string message, string? userMessage = null, string rawResponse = "") =>
        new(false, message, rawResponse) { UserMessage = userMessage };

    public static string FormatResponse(string value) =>
        string.Concat(value.Where(character => character is >= ' ' and <= '~'));
}

public sealed record SerialCommandResult(bool Succeeded, string? Value, string Message, string RawResponse)
{
    public static SerialCommandResult NotConnected() =>
        new(false, null, "The NovaSource G6 is not connected.", string.Empty);
}

public sealed class SerialPortProbeService : IDisposable
{
    private readonly ISerialPortProvider portProvider;
    private readonly TimeSpan responseTimeout;
    private readonly TimeSpan pollInterval;
    private readonly SemaphoreSlim transactionLock = new(1, 1);
    private ISerialPortConnection? activeConnection;
    private readonly SerialResponseBuffer responses = new();

    public string? ActivePortName { get; private set; }

    public bool IsConnected => activeConnection is not null;

    public SerialPortProbeService(
        ISerialPortProvider portProvider,
        TimeSpan? responseTimeout = null,
        TimeSpan? pollInterval = null)
    {
        this.portProvider = portProvider;
        this.responseTimeout = responseTimeout ?? TimeSpan.FromSeconds(5);
        this.pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(50);
    }

    public IReadOnlyList<string> GetAvailablePorts() =>
        portProvider.GetPortNames()
            .Where(portName => !string.IsNullOrWhiteSpace(portName))
            .OrderBy(portName => GetComPortNumber(portName) is null ? 1 : 0)
            .ThenBy(portName => GetComPortNumber(portName) ?? int.MaxValue)
            .ThenBy(portName => portName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static int? GetComPortNumber(string portName)
    {
        if (!portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(portName.AsSpan(3), out var portNumber))
        {
            return null;
        }

        return portNumber;
    }

    public async Task<SerialPortProbeResult> ProbeAsync(
        string? portName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return SerialPortProbeResult.Failure("Select a serial port before testing.", "Select a serial port, then click Connect.");
        }

        if (!GetAvailablePorts().Contains(portName, StringComparer.OrdinalIgnoreCase))
        {
            return SerialPortProbeResult.Failure($"{portName} is no longer available. Refresh the port list and try again.",
                $"{portName} is no longer available. Check the serial adapter connection, then click Refresh and select a port.");
        }

        var attempts = new System.Text.StringBuilder();
        try
        {
            Disconnect();
            var connection = portProvider.CreateConnection(portName);
            try
            {
                connection.Open();
                connection.DiscardInBuffer();
                var receivedAny = false;
                for (var attempt = 1; attempt <= 3; attempt++)
                {
                    attempts.AppendLine($"Attempt {attempt}:");
                    await DrainPendingInputAsync(connection, cancellationToken, attempts);
                    responses.Clear();
                    connection.Write("\r");
                    var raw = new System.Text.StringBuilder();
                    var elapsed = System.Diagnostics.Stopwatch.StartNew();
                    var invalidFrame = false;
                    while (elapsed.Elapsed < responseTimeout && !invalidFrame)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var input = connection.ReadExisting();
                        raw.Append(input);
                        responses.Append(input);
                        while (responses.TryRead(out var rawResponse))
                        {
                            if (string.IsNullOrWhiteSpace(rawResponse[..^1])) continue;
                            if (IsNovaSourcePrompt(rawResponse))
                            {
                                activeConnection = connection;
                                ActivePortName = portName;
                                return SerialPortProbeResult.Success(portName, rawResponse);
                            }
                            invalidFrame = true;
                        }
                        if (!invalidFrame) await Task.Delay(pollInterval, cancellationToken);
                    }
                    receivedAny |= raw.Length > 0;
                    attempts.AppendLine(raw.Length == 0 ? "No response received." : raw.ToString());
                    attempts.AppendLine(invalidFrame ? "Response was not a valid G6 prompt." : "Timed out waiting for a complete G6 prompt.");
                }
                return SerialPortProbeResult.Failure(
                    $"Could not obtain a valid NovaSource G6 prompt from {portName} after 3 attempts.",
                    receivedAny
                        ? $"{portName} responded, but a complete G6 reply couldn’t be confirmed after three attempts. Check the serial connection and selected device, then click Connect to try again."
                        : $"{portName} opened, but the G6 didn’t respond after three attempts. Check the device’s power and serial connection, then click Connect to try again.",
                    attempts.ToString());
            }
            finally
            {
                if (!ReferenceEquals(connection, activeConnection))
                {
                    connection.Dispose();
                }
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            return SerialPortProbeResult.Failure($"{portName} is already in use or access is denied. {exception.Message}",
                $"{portName} is in use or access was denied. Close any other application using this port, then click Connect to try again.");
        }
        catch (TimeoutException exception)
        {
            return SerialPortProbeResult.Failure(
                $"Timed out while sending the validation request to {portName}. {exception.Message}",
                $"Couldn’t communicate with the G6 on {portName}. Check the device’s power and serial connection, then click Connect to try again.", attempts.ToString());
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
        {
            return SerialPortProbeResult.Failure($"Could not validate {portName}: {exception.Message}",
                $"Couldn’t connect to the G6 on {portName}. Check that the G6 is powered on, the serial cable is connected, and the correct COM port is selected. Then click Connect to try again.", attempts.ToString());
        }
    }

    private async Task DrainPendingInputAsync(ISerialPortConnection connection, CancellationToken cancellationToken,
        System.Text.StringBuilder? diagnostics = null)
    {
        // A serial bridge can deliver a previous connection's reply after Open/Discard.
        // Require a quiet interval before sending the new validation request.
        var quietInterval = TimeSpan.FromMilliseconds(Math.Min(250, responseTimeout.TotalMilliseconds / 2));
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var lastInput = elapsed.Elapsed;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var input = connection.ReadExisting();
            if (input.Length > 0)
            {
                lastInput = elapsed.Elapsed;
                diagnostics?.AppendLine($"Delayed input before probe: {input}");
            }
            if (elapsed.Elapsed - lastInput >= quietInterval) return;
            if (elapsed.Elapsed >= responseTimeout)
                throw new IOException("The serial input did not become quiet before the connection probe.");
            await Task.Delay(pollInterval, cancellationToken);
        }
    }

    public async Task<SerialCommandResult> ExecuteCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        if (activeConnection is null)
        {
            return SerialCommandResult.NotConnected();
        }

        await transactionLock.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var connection = activeConnection;
            if (connection is null)
            {
                return SerialCommandResult.NotConnected();
            }

            // Buffered or newly arrived data predates this command and cannot acknowledge it.
            responses.Append(connection.ReadExisting());
            if (!string.IsNullOrWhiteSpace(responses.Pending))
                await DrainPendingInputAsync(connection, cancellationToken);
            responses.Clear();
            connection.Write(command.Trim() + "\r");
            var response = new System.Text.StringBuilder();
            var deadline = DateTime.UtcNow + responseTimeout;
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = connection.ReadExisting();
                response.Append(input);
                responses.Append(input);
                while (responses.TryRead(out var frame))
                {
                    if (string.IsNullOrWhiteSpace(frame[..^1])) continue;
                    return ParseCommandResponse(command, frame);
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            var partial = response.ToString();
            Disconnect();
            return new(false, null,
                string.IsNullOrEmpty(partial)
                    ? $"Timed out waiting for a response to {command}."
                    : $"Received an incomplete response to {command}: {SerialPortProbeResult.FormatResponse(partial)}",
                partial);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or TimeoutException)
        {
            Disconnect();
            return new(false, null, $"Serial command {command} failed: {exception.Message}", string.Empty);
        }
        catch (OperationCanceledException)
        {
            // A command may have reached the instrument. Do not reuse a stream with a late reply.
            Disconnect();
            throw;
        }
        finally
        {
            transactionLock.Release();
        }
    }

    public static SerialCommandResult ParseCommandResponse(string command, string response)
    {
        var promptIndex = response.IndexOf('>');
        var payload = promptIndex >= 0 ? response[..promptIndex].Trim(' ', '\r', '\n') : response;
        if (promptIndex >= 0 && (payload == "OK" || payload.StartsWith("OK ", StringComparison.Ordinal)))
        {
            var value = payload.Length > 2 ? payload[2..].Trim() : null;
            return new(true, string.IsNullOrEmpty(value) ? null : value,
                $"{command} completed successfully.", response);
        }

        if (promptIndex >= 0 && payload.StartsWith("ER ", StringComparison.Ordinal))
        {
            var meaning = payload switch
            {
                "ER 1" => "unknown command",
                "ER 2" => "value outside the legal range",
                "ER 3" => "device command failure",
                _ => "device error"
            };
            return new(false, null, $"{command} failed: {meaning} ({payload}).", response);
        }

        return new(false, null, $"{command} returned an unexpected response: {SerialPortProbeResult.FormatResponse(response)}", response);
    }

    public void Disconnect()
    {
        var connection = activeConnection;
        activeConnection = null;
        ActivePortName = null;
        responses.Clear();
        connection?.Dispose();
    }

    public void Dispose()
    {
        Disconnect();
        transactionLock.Dispose();
    }

    public static bool IsNovaSourcePrompt(string response)
    {
        var promptIndex = response.LastIndexOf('>');
        if (promptIndex < 0 || response[(promptIndex + 1)..].Any(character => character != ' '))
        {
            return false;
        }

        var payload = response[..promptIndex].Trim(' ', '\r', '\n');
        return payload == "OK" ||
               payload.StartsWith("OK ", StringComparison.Ordinal) ||
               (payload.StartsWith("NS G6 ", StringComparison.Ordinal) &&
                payload.Contains("(c) 2002 Nova Engineering, Inc.", StringComparison.Ordinal));
    }
}
