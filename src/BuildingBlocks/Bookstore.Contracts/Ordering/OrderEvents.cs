namespace Bookstore.Contracts.Ordering;

// Integration event published by Ordering when an order is accepted. It carries what consumers need
// (no Order aggregate, no EF Core types), so it can stay stable while the domain model changes.
// CustomerId is the identity provider's subject: consumers learn who acted from the message, never from a token.
public sealed record OrderPlaced(
    Guid OrderId, string CustomerId, string CustomerEmail, IReadOnlyList<OrderPlacedLine> Lines, decimal Total, string Currency,
    DateTimeOffset PlacedAtUtc);

public sealed record OrderPlacedLine(string Isbn, int Quantity, decimal UnitPrice);

// The two ends of the checkout saga.
public sealed record OrderConfirmed(Guid OrderId, string CustomerId, string CustomerEmail, DateTimeOffset ConfirmedAtUtc);

public sealed record OrderCancelled(Guid OrderId, string CustomerId, string CustomerEmail, string Reason, DateTimeOffset CancelledAtUtc);
