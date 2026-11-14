namespace Bookstore.ConcurrencyLab.Deadlocks;

public sealed class Warehouse(int id, string name)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public object Gate { get; } = new();
    public Dictionary<string, int> Stock { get; } = [];
}

public static class StockTransfer
{
    // Locks "from" first, then "to": two opposite transfers can wait for each other forever
    public static void Move(Warehouse from, Warehouse to, string isbn, int quantity)
    {
        lock (from.Gate)
        {
            lock (to.Gate)
            {
                Apply(from, to, isbn, quantity);
            }
        }
    }

    // Global lock order: always the smaller Id first, whatever the direction
    public static void MoveOrdered(Warehouse from, Warehouse to, string isbn, int quantity)
    {
        (Warehouse first, Warehouse second) = from.Id < to.Id ? (from, to) : (to, from);
        lock (first.Gate)
        {
            lock (second.Gate)
            {
                Apply(from, to, isbn, quantity);
            }
        }
    }

    // Timeout instead of waiting forever: the caller decides what to do on failure
    public static bool TryMove(Warehouse from, Warehouse to, string isbn, int quantity, TimeSpan timeout)
    {
        if (!Monitor.TryEnter(from.Gate, timeout))
            return false;
        try
        {
            if (!Monitor.TryEnter(to.Gate, timeout))
                return false;
            try
            {
                Apply(from, to, isbn, quantity);
                return true;
            }
            finally { Monitor.Exit(to.Gate); }
        }
        finally { Monitor.Exit(from.Gate); }
    }

    private static void Apply(Warehouse from, Warehouse to, string isbn, int quantity)
    {
        from.Stock[isbn] = from.Stock.GetValueOrDefault(isbn) - quantity;
        to.Stock[isbn] = to.Stock.GetValueOrDefault(isbn) + quantity;
    }
}
