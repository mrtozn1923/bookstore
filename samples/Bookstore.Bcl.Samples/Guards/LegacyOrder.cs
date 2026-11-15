using Bookstore.Bcl.Orders;

namespace Bookstore.Bcl.Samples.Guards;

// The constructor as it looked before this post: hand-written checks, one message style per developer.
public sealed class LegacyOrder
{
    public LegacyOrder(string orderNumber, string customerEmail, IReadOnlyList<OrderItem> items, DateTimeOffset placedAt)
    {
        if (orderNumber == null)
            throw new ArgumentNullException(nameof(orderNumber));
        if (orderNumber.Trim().Length == 0)
            throw new ArgumentException("Order number is empty");
        if (customerEmail == null || !customerEmail.Contains('@'))
            throw new ArgumentException("Invalid email", "email");
        if (items == null)
            throw new ArgumentNullException("items");
        if (items.Count == 0)
            throw new ArgumentOutOfRangeException(nameof(items), "Order must have items");
        if (placedAt == default)
            throw new ArgumentException(nameof(placedAt));

        OrderNumber = orderNumber;
        CustomerEmail = customerEmail;
        Items = items;
        PlacedAt = placedAt;
    }

    public string OrderNumber { get; }
    public string CustomerEmail { get; }
    public IReadOnlyList<OrderItem> Items { get; }
    public DateTimeOffset PlacedAt { get; }
}
