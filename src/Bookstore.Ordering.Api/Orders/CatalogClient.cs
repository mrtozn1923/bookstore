namespace Bookstore.Ordering.Api.Orders;

public sealed record CatalogBook(string Isbn, string Title, decimal Price, string Currency);

// Typed client; the base address is a logical name resolved by service discovery
public sealed class CatalogClient(HttpClient http)
{
    public async Task<IReadOnlyList<CatalogBook>> GetBooksAsync(IEnumerable<string> isbns, CancellationToken ct)
    {
        string query = string.Join("&", isbns.Distinct().Select(i => $"isbn={Uri.EscapeDataString(i)}"));
        return await http.GetFromJsonAsync<List<CatalogBook>>($"/api/books/lookup?{query}", ct) ?? [];
    }

    public async Task<CatalogBook?> GetBookAsync(string isbn, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.GetAsync($"/api/books/{isbn}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogBook>(ct);
    }
}
