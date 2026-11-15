using Bookstore.Admin.Client.Contracts;

namespace Bookstore.Admin.Client.Catalog;

// What an Auto component needs from the catalog. On the server it reads the database with EF Core,
// in the browser it calls the panel's own /api/catalog endpoint over HTTP.
public interface IBookCatalog
{
    Task<PagedResult<BookSummary>> SearchAsync(string? search, int page, int pageSize, CancellationToken ct = default);
}
