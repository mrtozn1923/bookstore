namespace Bookstore.Contracts.Catalog;

// Integration events published by Catalog. Version grows by one with every change to the same book,
// so a consumer can ignore an event that is older than the copy it already has.
public sealed record BookPriceChanged(
    string Isbn, decimal OldPrice, decimal NewPrice, string Currency, int Version, DateTimeOffset OccurredAtUtc);

public sealed record BookRenamed(
    string Isbn, string OldTitle, string NewTitle, int Version, DateTimeOffset OccurredAtUtc);
