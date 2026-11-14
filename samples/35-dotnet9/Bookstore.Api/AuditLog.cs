namespace Bookstore.Api;

public class AuditLog
{
    private readonly Lock _lock = new();
    private readonly List<string> _entries = [];

    public void Add(params ReadOnlySpan<string> messages)
    {
        lock (_lock)
        {
            foreach (string message in messages)
            {
                _entries.Add(message);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }
}
