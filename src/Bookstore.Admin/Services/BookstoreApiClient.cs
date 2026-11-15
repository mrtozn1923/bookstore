using System.Globalization;
using System.Net;
using Bookstore.Admin.Client.Contracts;
using Microsoft.AspNetCore.WebUtilities;

namespace Bookstore.Admin.Services;

// Typed HttpClient for Bookstore.Api; the base address comes from configuration (BookstoreApi:BaseUrl).
public sealed class BookstoreApiClient(HttpClient http)
{
    public async Task<PagedResult<BookSummary>> GetBooksAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        string uri = QueryHelpers.AddQueryString("api/books", new Dictionary<string, string?>
        {
            ["search"] = search,
            ["category"] = category,
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        });
        return await http.GetFromJsonAsync<PagedResult<BookSummary>>(uri, ct)
            ?? new PagedResult<BookSummary>([], 0, page, pageSize);
    }

    public async Task<IReadOnlyList<CategoryView>> GetCategoriesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<CategoryView>>("api/categories", ct) ?? [];

    public Task<BookDetails?> GetBookAsync(string isbn, CancellationToken ct = default) =>
        GetOrNullAsync<BookDetails>($"api/books/{Uri.EscapeDataString(isbn)}", ct);

    public Task<OrderView?> GetOrderAsync(int orderId, CancellationToken ct = default) =>
        GetOrNullAsync<OrderView>($"api/orders/{orderId}", ct);

    public async Task<IReadOnlyList<StockView>> GetStockAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<StockView>>("api/inventory", ct) ?? [];

    // False when the API rejects the value (400), for example a count below the reserved quantity.
    public async Task<bool> SetQuantityOnHandAsync(string isbn, int quantityOnHand, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.PutAsJsonAsync(
            $"api/inventory/{Uri.EscapeDataString(isbn)}", new SetStockRequest(quantityOnHand), ct);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    // Null when the author was created; otherwise the API's validation errors by key, for example "Books[1].Isbn".
    public async Task<IDictionary<string, string[]>?> CreateAuthorAsync(NewAuthorRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync("api/authors", request, ct);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            HttpValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ct);
            return problem?.Errors ?? new Dictionary<string, string[]>();
        }

        response.EnsureSuccessStatusCode();
        return null;
    }

    private async Task<T?> GetOrNullAsync<T>(string uri, CancellationToken ct) where T : class
    {
        using HttpResponseMessage response = await http.GetAsync(uri, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }
}
