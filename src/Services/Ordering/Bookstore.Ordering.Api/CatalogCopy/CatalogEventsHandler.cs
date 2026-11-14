using Bookstore.Contracts.Catalog;
using Bookstore.Ordering.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.CatalogCopy;

// Keeps Ordering's copy of the catalog current. RabbitMQ delivers at least once and, after a retry
// or a redelivery, not necessarily in order: the version decides whether an event is still news.
public static class CatalogEventsHandler
{
    public static async Task Handle(
        BookPriceChanged message, OrderingDbContext db, TimeProvider clock, ILogger logger, CancellationToken ct)
    {
        CatalogBook book = await FindAsync(db, message.Isbn, ct);
        if (IsStale(book.Isbn, message.Version, book.PriceVersion, nameof(BookPriceChanged), logger))
        {
            return;
        }
        if (message.Currency != book.Currency)
        {
            throw new InvalidOperationException($"Price of {message.Isbn} came in {message.Currency}, the copy is in {book.Currency}.");
        }

        book.Price = message.NewPrice;
        book.PriceVersion = message.Version;
        book.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        logger.LogInformation("Copy of {Isbn} now costs {Price} (v{Version})", book.Isbn, book.Price, message.Version);
        // No SaveChanges here: Wolverine's transaction middleware commits it together with the inbox row.
    }

    public static async Task Handle(
        BookRenamed message, OrderingDbContext db, TimeProvider clock, ILogger logger, CancellationToken ct)
    {
        CatalogBook book = await FindAsync(db, message.Isbn, ct);
        if (IsStale(book.Isbn, message.Version, book.TitleVersion, nameof(BookRenamed), logger))
        {
            return;
        }

        book.Title = message.NewTitle;
        book.TitleVersion = message.Version;
        book.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        logger.LogInformation("Copy of {Isbn} renamed to {Title} (v{Version})", book.Isbn, book.Title, message.Version);
    }

    private static async Task<CatalogBook> FindAsync(OrderingDbContext db, string isbn, CancellationToken ct) =>
        await db.CatalogBooks.FirstOrDefaultAsync(b => b.Isbn == isbn, ct)
        ?? throw new InvalidOperationException($"Book {isbn} is not in Ordering's catalog copy.");

    // Every change in Catalog increments the version, so an event that is not newer than the field's version
    // is a duplicate or arrived late. Ignoring it is safe: the copy already has that change or a later one.
    private static bool IsStale(string isbn, int version, int copyVersion, string eventType, ILogger logger)
    {
        if (version > copyVersion)
        {
            return false;
        }
        logger.LogInformation("Ignoring {EventType} v{Version} for {Isbn}: copy is already at v{CopyVersion}",
            eventType, version, isbn, copyVersion);
        return true;
    }
}
