namespace Bookstore.Ordering.Domain;

public enum OrderStatus
{
    Pending,
    StockReserved,
    Cancelled
}

public sealed record OrderLine(string Isbn, string Title, Money UnitPrice, int Quantity);

public sealed record OrderPlaced(OrderId OrderId, Money Total, int ItemCount, DateTimeOffset OccurredAtUtc);

public sealed record OrderCancelled(OrderId OrderId, string Reason, DateTimeOffset OccurredAtUtc);

public sealed class Order
{
    public const int MaxQuantityPerBook = 10;

    private readonly List<OrderItem> _items = [];
    private readonly List<object> _domainEvents = [];

    private Order() { } // EF Core

    public OrderId Id { get; private set; }
    public Buyer Buyer { get; private set; } = null!;
    public DateTime PlacedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? StockReservationId { get; private set; }
    public DateTime? StockReservedUntilUtc { get; private set; }
    public Money Total { get; private set; } = null!;
    public IReadOnlyList<OrderItem> Items => _items;
    public IReadOnlyList<object> DomainEvents => _domainEvents;

    public static Order Place(Buyer buyer, IReadOnlyCollection<OrderLine> lines, TimeProvider clock)
    {
        if (lines.Count == 0)
        {
            throw new DomainException("order.empty", "An order needs at least one item.");
        }

        DateTimeOffset now = clock.GetUtcNow();
        Order order = new()
        {
            Id = OrderId.New(),
            Buyer = buyer,
            PlacedAtUtc = now.UtcDateTime,
            Status = OrderStatus.Pending
        };
        foreach (OrderLine line in lines)
        {
            order.Add(line);
        }
        order.Total = order._items.Select(i => i.LineTotal).Aggregate((sum, lineTotal) => sum + lineTotal);

        // Pending, not confirmed: stock and payment are not settled yet (checkout saga, post 07).
        order._domainEvents.Add(new OrderPlaced(order.Id, order.Total, order._items.Sum(i => i.Quantity), now));
        return order;
    }

    public void MarkStockReserved(Guid reservationId, DateTime reservedUntilUtc)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("order.not-reservable", $"An order in status {Status} cannot get a stock reservation.");
        }
        Status = OrderStatus.StockReserved;
        StockReservationId = reservationId;
        StockReservedUntilUtc = reservedUntilUtc;
    }

    public void Cancel(string reason, TimeProvider clock)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.StockReserved))
        {
            throw new DomainException("order.not-cancellable", $"An order in status {Status} cannot be cancelled.");
        }
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        _domainEvents.Add(new OrderCancelled(Id, reason, clock.GetUtcNow()));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Add(OrderLine line)
    {
        if (line.Quantity < 1)
        {
            throw new DomainException("order.invalid-quantity", $"Quantity for {line.Isbn} must be at least 1.");
        }
        if (_items.Count > 0 && _items[0].UnitPrice.Currency != line.UnitPrice.Currency)
        {
            throw new DomainException("order.mixed-currency", "All items of an order must have the same currency.");
        }

        OrderItem? existing = _items.Find(i => i.Isbn == line.Isbn);
        int quantity = (existing?.Quantity ?? 0) + line.Quantity;
        if (quantity > MaxQuantityPerBook)
        {
            throw new DomainException("order.too-many-copies", $"At most {MaxQuantityPerBook} copies of {line.Isbn} per order.");
        }

        if (existing is null)
        {
            _items.Add(new OrderItem(line.Isbn, line.Title, line.UnitPrice, line.Quantity));
        }
        else
        {
            existing.ChangeQuantity(quantity);
        }
    }
}

public sealed class OrderItem
{
    private OrderItem() { } // EF Core

    internal OrderItem(string isbn, string title, Money unitPrice, int quantity)
    {
        Isbn = isbn;
        Title = title;
        UnitPrice = unitPrice;
        ChangeQuantity(quantity);
    }

    public string Isbn { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money LineTotal { get; private set; } = null!;

    internal void ChangeQuantity(int quantity)
    {
        Quantity = quantity;
        LineTotal = UnitPrice.Times(quantity);
    }
}
