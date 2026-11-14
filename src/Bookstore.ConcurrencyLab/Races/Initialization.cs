namespace Bookstore.ConcurrencyLab.Races;

// Order violation: the worker thread may run before _carriers is assigned
public sealed class ShipmentTracker
{
    private readonly Dictionary<string, string> _carriers;
    private readonly Thread _worker;
    public Exception? WorkerError { get; private set; }

    public ShipmentTracker()
    {
        _worker = new Thread(Poll);
        _worker.Start();
        _carriers = new() { ["TR"] = "Yurtici", ["DE"] = "DHL" };
    }

    private void Poll()
    {
        try { _ = _carriers["TR"]; }
        catch (Exception ex) { WorkerError = ex; }
    }

    public void Wait() => _worker.Join();
}

public sealed class SearchIndex
{
    public static int Created;
    public SearchIndex() => Interlocked.Increment(ref Created);
}

public static class IndexHolder
{
    private static SearchIndex? _index;

    // Lazy initialization race: two threads can both see null
    public static SearchIndex Get()
    {
        if (_index is null)
            _index = new SearchIndex();
        return _index;
    }

    public static void Reset() => _index = null;
}
