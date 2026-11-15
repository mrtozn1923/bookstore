using System.Net;
using Bookstore.Admin.Client.Contracts;

namespace Bookstore.Admin.Services;

// Typed HttpClient for Bookstore.Api; the base address comes from configuration (BookstoreApi:BaseUrl).
public sealed class BookstoreApiClient(HttpClient http)
{
    public async Task<PagedResult<BookSummary>> GetBooksAsync(int page, int pageSize, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<PagedResult<BookSummary>>($"api/books?page={page}&pageSize={pageSize}", ct)
        ?? new PagedResult<BookSummary>([], 0, page, pageSize);

    public Task<BookDetails?> GetBookAsync(string isbn, CancellationToken ct = default) =>
        GetOrNullAsync<BookDetails>($"api/books/{Uri.EscapeDataString(isbn)}", ct);

    public Task<OrderView?> GetOrderAsync(int orderId, CancellationToken ct = default) =>
        GetOrNullAsync<OrderView>($"api/orders/{orderId}", ct);

    public async Task<IReadOnlyList<StockView>> GetStockAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<StockView>>("api/inventory", ct) ?? [];

    public async Task SetQuantityOnHandAsync(string isbn, int quantityOnHand, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.PutAsJsonAsync(
            $"api/inventory/{Uri.EscapeDataString(isbn)}", new SetStockRequest(quantityOnHand), ct);
        response.EnsureSuccessStatusCode();
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
