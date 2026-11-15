using Bookstore.Admin.Client.Auth;
using Bookstore.Admin.Client.Catalog;
using Bookstore.Admin.Client.Contracts;

namespace Bookstore.Admin.Services;

// The endpoint HttpBookCatalog calls from WebAssembly. The browser sends the panel's cookie with it (same origin),
// so the same staff policy as the pages applies; a hidden button in the UI would protect nothing.
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapPanelCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalog/books", (IBookCatalog catalog, string? search, CancellationToken ct, int page = 1, int pageSize = 10)
            => catalog.SearchAsync(search, page, pageSize, ct))
            .RequireAuthorization(PanelPolicies.Staff);
        return app;
    }
}
