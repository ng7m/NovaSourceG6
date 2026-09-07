using System.Text.Json;

namespace NovaSourceG6Config;

public sealed class JsonConnectionProfileStore : IConnectionProfileStore
{
    private readonly string filePath;

    public JsonConnectionProfileStore(string? filePath = null)
    {
        this.filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaSourceG6Config",
            "connection-profile.json");
    }

    public ConnectionProfile? Load()
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            return JsonSerializer.Deserialize<ConnectionProfile>(File.ReadAllText(filePath));
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save(ConnectionProfile profile)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
    }
}
