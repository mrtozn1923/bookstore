using System.ComponentModel.DataAnnotations;

namespace Bookstore.Ordering.Orders;

// UnitPrice is the price the customer saw in the cart. Ordering compares it with its own copy.
public sealed record PlaceOrderItemRequest(
    [property: Required, StringLength(13, MinimumLength = 13)] string Isbn,
    [property: Range(1, 99)] int Quantity,
    [property: Range(typeof(decimal), "0.01", "100000")] decimal UnitPrice);

// Who orders is not part of the request any more: the buyer comes from the access token.
public sealed record PlaceOrderRequest(
    [property: Required, MinLength(1), MaxLength(50)] List<PlaceOrderItemRequest> Items);

public sealed record OrderLineResponse(string Isbn, string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

// CancellationReason is new in post 11 (an added field, not a breaking change): the UI tells the customer why.
public sealed record OrderResponse(
    Guid Id, string Status, DateTime PlacedAtUtc, string CustomerName, string CustomerEmail,
    IReadOnlyList<OrderLineResponse> Items, decimal Total, string Currency, DateTime? StockReservedUntilUtc,
    string? CancellationReason);

public sealed record OrderSummaryResponse(Guid Id, string Status, DateTime PlacedAtUtc, int ItemCount, decimal Total, string Currency);

public sealed record PriceChange(string Isbn, decimal CartPrice, decimal CurrentPrice);
