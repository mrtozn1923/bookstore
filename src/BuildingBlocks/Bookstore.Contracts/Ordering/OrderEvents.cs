namespace Bookstore.Contracts.Ordering;

// Integration event published by Ordering when an order is accepted. It carries what consumers need
// (no Order aggregate, no EF Core types), so it can stay stable while the domain model changes.
public sealed record OrderPlaced(
    Guid OrderId, string CustomerEmail, IReadOnlyList<OrderPlacedLine> Lines, decimal Total, string Currency,
    DateTimeOffset PlacedAtUtc);

public sealed record OrderPlacedLine(string Isbn, int Quantity, decimal UnitPrice);
