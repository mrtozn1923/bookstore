using System.ComponentModel.DataAnnotations;

namespace Bookstore.Ordering;

public sealed record CartLineView(string Isbn, string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record CartView(Guid Id, IReadOnlyList<CartLineView> Items, decimal Total, string Currency);

public sealed record SetQuantityRequest([property: Range(1, 99)] int Quantity);

public sealed record PlaceOrderRequest(
    Guid CartId,
    [property: Required, StringLength(200)] string CustomerName,
    [property: Required, EmailAddress, StringLength(256)] string CustomerEmail,
    [property: StringLength(20)] string? InstitutionCode = null);

public sealed record OrderLineView(string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderView(
    int Id, string Status, DateTime PlacedAtUtc, IReadOnlyList<OrderLineView> Items, decimal Total, string Currency);
