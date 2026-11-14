namespace Bookstore.Ordering;

public enum OrderStatus
{
    Placed,
    PaymentPending,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public DateTime PlacedAtUtc { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; } = [];
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public required string Title { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    // Each line is rounded to two decimals, midpoint away from zero (Requirements Analysis series).
    public static decimal CalculateLineTotal(decimal unitPrice, int quantity) =>
        Math.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
}
