namespace Bookstore.Inventory;

public interface IStockLedger
{
    Task ReserveAsync(string isbn, int quantity, CancellationToken ct);
}

public sealed class InsufficientStockException(string isbn, int requested, int available)
    : InvalidOperationException($"Not enough stock for {isbn}: requested {requested}, available {available}.")
{
    public string Isbn { get; } = isbn;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}

public sealed class InMemoryStockLedger : IStockLedger
{
    private readonly Dictionary<string, int> onHand = [];

    public InMemoryStockLedger Receive(string isbn, int quantity)
    {
        onHand[isbn] = Available(isbn) + quantity;
        return this;
    }

    public int Available(string isbn) => onHand.GetValueOrDefault(isbn);

    public Task ReserveAsync(string isbn, int quantity, CancellationToken ct)
    {
        int available = Available(isbn);
        if (quantity > available)
        {
            return Task.FromException(new InsufficientStockException(isbn, quantity, available));
        }

        onHand[isbn] = available - quantity;
        return Task.CompletedTask;
    }
}
