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
    public static SerialPortProbeResult Success(string portName, string response) =>
        new(true, $"{portName} returned the NovaSource G6 command prompt: {FormatResponse(response)}", response);

    public static SerialPortProbeResult Failure(string message) => new(false, message);

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
            return SerialPortProbeResult.Failure("Select a serial port before testing.");
        }

        if (!GetAvailablePorts().Contains(portName, StringComparer.OrdinalIgnoreCase))
        {
            return SerialPortProbeResult.Failure($"{portName} is no longer available. Refresh the port list and try again.");
        }

        try
        {
            Disconnect();
            var connection = portProvider.CreateConnection(portName);
            try
            {
                connection.Open();
                connection.DiscardInBuffer();
                connection.Write("\r");

                var response = new System.Text.StringBuilder();
                var deadline = DateTime.UtcNow + responseTimeout;
                while (DateTime.UtcNow < deadline)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    response.Append(connection.ReadExisting());

                    if (response.ToString().Contains('>'))
                    {
                        var rawResponse = response.ToString();
                        if (IsNovaSourcePrompt(rawResponse))
                        {
                            activeConnection = connection;
                            ActivePortName = portName;
                            return SerialPortProbeResult.Success(portName, rawResponse);
                        }

                        return SerialPortProbeResult.Failure(
                            $"{portName} responded, but the response was not a NovaSource G6 prompt: {SerialPortProbeResult.FormatResponse(rawResponse)}");
                    }

                    await Task.Delay(pollInterval, cancellationToken);
                }

                var partialResponse = response.ToString();
                return string.IsNullOrEmpty(partialResponse)
                    ? SerialPortProbeResult.Failure($"Timed out waiting for a NovaSource G6 prompt from {portName}.")
                    : SerialPortProbeResult.Failure(
                        $"Timed out after receiving an incomplete response from {portName}: {SerialPortProbeResult.FormatResponse(partialResponse)}");
            }
            finally
            {
                if (!ReferenceEquals(connection, activeConnection))
                {
                    connection.Dispose();
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return SerialPortProbeResult.Failure($"{portName} is already in use or access is denied.");
        }
        catch (TimeoutException)
        {
            return SerialPortProbeResult.Failure(
                $"Timed out while sending the validation request to {portName}.");
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
        {
            return SerialPortProbeResult.Failure($"Could not validate {portName}: {exception.Message}");
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
            var connection = activeConnection;
            if (connection is null)
            {
                return SerialCommandResult.NotConnected();
            }

            connection.DiscardInBuffer();
            connection.Write(command.Trim() + "\r");
            var response = new System.Text.StringBuilder();
            var deadline = DateTime.UtcNow + responseTimeout;
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                response.Append(connection.ReadExisting());
                if (response.ToString().Contains('>'))
                {
                    return ParseCommandResponse(command, response.ToString());
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            var partial = response.ToString();
            return new(false, null,
                string.IsNullOrEmpty(partial)
                    ? $"Timed out waiting for a response to {command}."
                    : $"Received an incomplete response to {command}: {SerialPortProbeResult.FormatResponse(partial)}",
                partial);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or TimeoutException)
        {
            return new(false, null, $"Serial command {command} failed: {exception.Message}", string.Empty);
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
        if (payload.StartsWith("OK", StringComparison.Ordinal))
        {
            var value = payload.Length > 2 ? payload[2..].Trim() : null;
            return new(true, string.IsNullOrEmpty(value) ? null : value,
                $"{command} completed successfully.", response);
        }

        if (payload.StartsWith("ER ", StringComparison.Ordinal))
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
        activeConnection?.Dispose();
        activeConnection = null;
        ActivePortName = null;
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

        var content = response[..promptIndex].TrimEnd(' ');
        if (!content.EndsWith('\r'))
        {
            return false;
        }

        var payload = content[..^1];
        return payload == "OK" ||
               payload.StartsWith("OK ", StringComparison.Ordinal) ||
               (payload.StartsWith("NS G6 ", StringComparison.Ordinal) &&
                payload.Contains("(c) 2002 Nova Engineering, Inc.", StringComparison.Ordinal));
    }
}
