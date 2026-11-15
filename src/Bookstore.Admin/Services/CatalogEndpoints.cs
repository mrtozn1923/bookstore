using Bookstore.Admin.Client.Catalog;
using Bookstore.Admin.Client.Contracts;

namespace Bookstore.Admin.Services;

// The endpoint HttpBookCatalog calls from the browser. Like the inventory endpoints, it is protected in post 11.
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapPanelCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalog/books", (IBookCatalog catalog, string? search, CancellationToken ct, int page = 1, int pageSize = 10)
            => catalog.SearchAsync(search, page, pageSize, ct));
        return app;
    }
}
