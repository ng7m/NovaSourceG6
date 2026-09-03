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
}

public sealed record SerialPortProbeResult(bool Succeeded, string Message)
{
    public static SerialPortProbeResult Success(string portName) =>
        new(true, $"{portName} opened and closed successfully. No data was sent.");

    public static SerialPortProbeResult Failure(string message) => new(false, message);
}

public sealed class SerialPortProbeService(ISerialPortProvider portProvider)
{
    public IReadOnlyList<string> GetAvailablePorts() =>
        portProvider.GetPortNames()
            .Where(portName => !string.IsNullOrWhiteSpace(portName))
            .OrderBy(portName => portName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public SerialPortProbeResult Probe(string? portName)
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
            using var connection = portProvider.CreateConnection(portName);
            connection.Open();
            return SerialPortProbeResult.Success(portName);
        }
        catch (UnauthorizedAccessException)
        {
            return SerialPortProbeResult.Failure($"{portName} is already in use or access is denied.");
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
        {
            return SerialPortProbeResult.Failure($"Could not open {portName}: {exception.Message}");
        }
    }
}
