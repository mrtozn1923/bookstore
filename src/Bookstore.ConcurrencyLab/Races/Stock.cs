using System.Collections.Concurrent;

namespace Bookstore.ConcurrencyLab.Races;

public sealed class StockItem(string isbn, int available)
{
    public string Isbn { get; } = isbn;
    public int Available { get; private set; } = available;
    private readonly Lock _gate = new();

    // Check-then-act: the check may be stale when the act runs
    public bool TryReserve()
    {
        if (Available > 0)
        {
            Available--;
            return true;
        }
        return false;
    }

    // Check and act in the same critical section
    public bool TryReserveLocked()
    {
        lock (_gate)
        {
            if (Available > 0)
            {
                Available--;
                return true;
            }
            return false;
        }
    }
}

public sealed class CatalogStock
{
    private readonly ConcurrentDictionary<string, int> _available = new();

    public void Set(string isbn, int count) => _available[isbn] = count;
    public int Get(string isbn) => _available[isbn];

    // Every single call is thread-safe; the sequence of calls is not
    public bool TryReserve(string isbn)
    {
        if (_available[isbn] > 0)
        {
            _available[isbn] = _available[isbn] - 1;
            return true;
        }
        return false;
    }

    // One atomic compare-and-update decides the whole step
    public bool TryReserveAtomic(string isbn)
    {
        while (true)
        {
            int current = _available[isbn];
            if (current == 0)
                return false;
            if (_available.TryUpdate(isbn, current - 1, current))
                return true;
        }
    }
}
