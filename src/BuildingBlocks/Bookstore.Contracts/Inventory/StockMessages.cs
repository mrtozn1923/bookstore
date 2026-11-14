namespace Bookstore.Contracts.Inventory;

// Commands Ordering's checkout saga sends to Inventory. OrderId makes every one of them idempotent.
public sealed record ReserveStock(Guid OrderId, IReadOnlyList<StockLine> Lines);

public sealed record StockLine(string Isbn, int Quantity);

public sealed record CommitStock(Guid OrderId);

public sealed record ReleaseStock(Guid OrderId, string Reason);

// Inventory's answers, published as events.
public sealed record StockReserved(Guid OrderId, Guid ReservationId, DateTimeOffset ExpiresAtUtc);

public sealed record StockReservationFailed(Guid OrderId, IReadOnlyList<string> UnavailableIsbns);

public sealed record StockCommitted(Guid OrderId);
