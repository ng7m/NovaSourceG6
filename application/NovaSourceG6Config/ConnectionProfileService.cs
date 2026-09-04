namespace NovaSourceG6Config;

public sealed record ConnectionProfileResult(bool Succeeded, string Message, ConnectionProfile? Profile = null);

public sealed class ConnectionProfileService(IConnectionProfileStore store)
{
    private static readonly HashSet<int> SupportedBaudRates = [9600, 19200, 38400, 57600, 115200];

    public ConnectionProfile? Load() => store.Load();

    public ConnectionProfileResult Save(string? name, string? portName, string? baudRate)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return new(false, "Select a serial port before saving the connection profile.");

        if (!int.TryParse(baudRate, out var parsedBaudRate) || !SupportedBaudRates.Contains(parsedBaudRate))
            return new(false, "Select a supported baud rate.");

        var profile = new ConnectionProfile(name?.Trim() ?? string.Empty, portName, parsedBaudRate);
        try
        {
            store.Save(profile);
            return new(true, $"Saved {profile.DisplayName} at {profile.BaudRate} baud.", profile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(false, $"Could not save the connection profile: {exception.Message}");
        }
    }
}
