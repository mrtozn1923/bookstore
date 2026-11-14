namespace Bookstore.Ordering.Api.Orders;

public sealed record PlaceOrderRequest(string CustomerEmail, IReadOnlyList<OrderItemRequest> Items);

public sealed record OrderItemRequest(string Isbn, int Quantity);

public sealed record OrderLine(string Isbn, string Title, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record Order(Guid Id, string CustomerEmail, IReadOnlyList<OrderLine> Lines, decimal Total, string Currency, DateTimeOffset PlacedAtUtc);
