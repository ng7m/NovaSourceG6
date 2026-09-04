namespace NovaSourceG6Config;

public sealed record ConnectionProfile(string Name, string PortName, int BaudRate = 38400)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? PortName : $"{Name.Trim()} ({PortName})";
}

public interface IConnectionProfileStore
{
    ConnectionProfile? Load();
    void Save(ConnectionProfile profile);
}
