using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

// The only door other modules use to read the catalog.
public sealed record BookInfo(int Id, string Isbn, string Title, decimal Price, int CategoryId);

public interface ICatalogService
{
    Task<BookInfo?> FindByIsbnAsync(string isbn, CancellationToken ct);
    Task<IReadOnlyDictionary<int, BookInfo>> GetByIdsAsync(IReadOnlyCollection<int> bookIds, CancellationToken ct);
}

internal sealed class CatalogService(BookstoreContext db) : ICatalogService
{
    public Task<BookInfo?> FindByIsbnAsync(string isbn, CancellationToken ct) =>
        db.Books.AsNoTracking()
            .Where(b => b.Isbn == isbn)
            .Select(b => new BookInfo(b.Id, b.Isbn, b.Title, b.Price, b.CategoryId))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<int, BookInfo>> GetByIdsAsync(IReadOnlyCollection<int> bookIds, CancellationToken ct) =>
        await db.Books.AsNoTracking()
            .Where(b => bookIds.Contains(b.Id))
            .Select(b => new BookInfo(b.Id, b.Isbn, b.Title, b.Price, b.CategoryId))
            .ToDictionaryAsync(b => b.Id, ct);
}
