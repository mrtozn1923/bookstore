using Bookstore.Admin.Client.Catalog;
using Bookstore.Admin.Client.Contracts;
using Bookstore.Catalog;
using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Admin.Services;

// The server implementation: reads the catalog straight from the database. One short-lived DbContext per call,
// so two components on the same circuit never share a context.
public sealed class EfBookCatalog(IDbContextFactory<BookstoreContext> dbFactory) : IBookCatalog
{
    public async Task<PagedResult<BookSummary>> SearchAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        await using BookstoreContext db = await dbFactory.CreateDbContextAsync(ct);

        IQueryable<Book> query = db.Books;
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(b => b.Title.Contains(term) || b.Author.Name.Contains(term) || b.Isbn == term);
        }

        int total = await query.CountAsync(ct);
        List<BookSummary> items = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookSummary(b.Isbn, b.Title, b.Author.Name, b.Category.Name, b.Price, b.PublishedYear))
            .ToListAsync(ct);
        return new PagedResult<BookSummary>(items, total, page, pageSize);
    }
}
