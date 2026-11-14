using System.ComponentModel.DataAnnotations;

namespace Bookstore.Ordering;

public sealed record CartLineView(string Isbn, string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record CartView(
    Guid Id,
    IReadOnlyList<CartLineView> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    string Currency,
    string? CouponCode,
    string? CouponDescription);

public sealed record SetQuantityRequest([property: Range(1, 99)] int Quantity);

public sealed record ApplyCouponRequest([property: Required, StringLength(32)] string Code);

public sealed record PlaceOrderRequest(
    Guid CartId,
    [property: Required, StringLength(200)] string CustomerName,
    [property: Required, EmailAddress, StringLength(256)] string CustomerEmail);

public sealed record OrderLineView(string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderView(
    int Id,
    string Status,
    DateTime PlacedAtUtc,
    IReadOnlyList<OrderLineView> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    string Currency,
    string? CouponCode);
