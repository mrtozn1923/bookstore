using System.Globalization;
using System.Net.Http.Json;
using Bookstore.Admin.Client.Contracts;

namespace Bookstore.Admin.Client.Catalog;

// The WebAssembly implementation: same origin as the page, so no CORS and no API address in the browser.
public sealed class HttpBookCatalog(HttpClient http) : IBookCatalog
{
    public async Task<PagedResult<BookSummary>> SearchAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        string uri = $"api/catalog/books?page={page.ToString(CultureInfo.InvariantCulture)}&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}"
            + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search)}");
        return await http.GetFromJsonAsync<PagedResult<BookSummary>>(uri, ct)
            ?? new PagedResult<BookSummary>([], 0, page, pageSize);
    }
}
