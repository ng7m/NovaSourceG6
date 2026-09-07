namespace NovaSourceG6Config;

public sealed class SerialResponseBuffer
{
    private string pending = string.Empty;
    public string Pending => pending;
    public void Append(string input) => pending += input;
    public void Clear() => pending = string.Empty;

    public bool TryRead(out string response)
    {
        var end = pending.IndexOf('>');
        if (end < 0) { response = string.Empty; return false; }
        response = pending[..(end + 1)];
        pending = pending[(end + 1)..];
        return true;
    }
}
