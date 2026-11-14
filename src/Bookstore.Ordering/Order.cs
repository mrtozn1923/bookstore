namespace Bookstore.Ordering;

public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

public sealed record OrderItem(string Isbn, string Title, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed record Order(
    string OrderNumber,
    string CustomerName,
    DateTime CreatedAtUtc,
    OrderStatus Status,
    string Currency,
    IReadOnlyList<OrderItem> Items)
{
    public decimal Total => Items.Sum(i => i.LineTotal);
}
