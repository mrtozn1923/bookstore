using System.Collections.Concurrent;
using Bookstore.Contracts;

namespace Bookstore.Inventory;

// Stock stays in memory: the warehouse model is not this series' topic
public sealed class StockLedger
{
    private const int InitialStock = 100;
    private readonly ConcurrentDictionary<string, int> reserved = new();

    public void Reserve(IEnumerable<OrderPlacedItem> items)
    {
        foreach (OrderPlacedItem item in items)
        {
            reserved.AddOrUpdate(item.Isbn, item.Quantity, (_, current) => current + item.Quantity);
        }
    }

    public int Available(string isbn) => InitialStock - reserved.GetValueOrDefault(isbn);
}
