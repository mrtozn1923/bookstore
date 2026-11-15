using Bookstore.Bcl.Text;

namespace Bookstore.Bcl.Orders;

public sealed record OrderItem
{
    public const int MaxQuantity = 500;

    public OrderItem(Isbn isbn, int quantity, decimal unitPrice)
    {
        Isbn = Guard.NotDefault(isbn);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, MaxQuantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Isbn Isbn { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed class Order
{
    public Order(string orderNumber, string customerEmail, IReadOnlyList<OrderItem> items, DateTimeOffset placedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        CustomerEmail = Guard.Email(customerEmail);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfZero(items.Count, nameof(items));
        ArgumentOutOfRangeException.ThrowIfEqual(placedAt, default);

        OrderNumber = orderNumber;
        Items = [.. items];
        PlacedAt = placedAt;
    }

    public string OrderNumber { get; }
    public string CustomerEmail { get; }
    public IReadOnlyList<OrderItem> Items { get; }
    public DateTimeOffset PlacedAt { get; }
    public decimal Total => Items.Sum(item => item.LineTotal);
}
