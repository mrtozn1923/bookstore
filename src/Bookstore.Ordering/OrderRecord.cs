namespace Bookstore.Ordering;

// Entities are equal when their Ids are equal
public abstract class Entity
{
    public Guid Id { get; init; }

    public override bool Equals(object? obj) => obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}

public sealed class OrderRecord : Entity
{
    public required string OrderNumber { get; init; }
    public required string CustomerEmail { get; init; }
    public OrderStatus Status { get; set; }
    public List<OrderLineRecord> Lines { get; init; } = [];
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string CreatedBy { get; init; } = "";
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public sealed class OrderLineRecord : Entity
{
    public required string Isbn { get; init; }
    public required string Title { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed record OrderLineDto(string Isbn, string Title, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    string OrderNumber, string CustomerEmail, OrderStatus Status, IReadOnlyList<OrderLineDto> Lines, decimal Total);

public static class OrderMapper
{
    public static OrderDto ToDto(OrderRecord order)
    {
        List<OrderLineDto> lines =
        [
            .. order.Lines
                .OrderBy(l => l.Title, StringComparer.Ordinal)
                .Select(l => new OrderLineDto(l.Isbn, l.Title, l.Quantity, Math.Round(l.UnitPrice, 2, MidpointRounding.AwayFromZero))),
        ];

        return new OrderDto(order.OrderNumber, order.CustomerEmail, order.Status, lines, lines.Sum(l => l.UnitPrice * l.Quantity));
    }
}
