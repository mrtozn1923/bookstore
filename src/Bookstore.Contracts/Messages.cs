namespace Bookstore.Contracts;

// Queue names are part of the contract between services
public static class Queues
{
    public const string OrderPlaced = "ordering.order-placed";
    public const string StockReserved = "inventory.stock-reserved";
}

public sealed record OrderPlaced(Guid OrderId, IReadOnlyList<OrderPlacedItem> Items, DateTimeOffset PlacedAtUtc);

public sealed record OrderPlacedItem(string Isbn, int Quantity);

public sealed record StockReserved(Guid OrderId, DateTimeOffset ReservedAtUtc);
