using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Mcp;

public sealed record BookSummary(string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear);

public sealed record BookDetails(
    string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear, string Description, int Available);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record InstitutionView(int Id, string Name, string Code, string Kind);

// Typed client for the GET endpoints of Bookstore.Api; no writes.
public sealed class BookstoreApi(HttpClient http)
{
    public async Task<PagedResult<BookSummary>> SearchBooksAsync(string query, string? category, CancellationToken ct)
    {
        string url = $"/api/books?search={Uri.EscapeDataString(query)}&pageSize=10";
        if (!string.IsNullOrWhiteSpace(category))
        {
            url += $"&category={Uri.EscapeDataString(category)}";
        }
        return await http.GetFromJsonAsync<PagedResult<BookSummary>>(url, ct)
            ?? new PagedResult<BookSummary>([], 0, 1, 10);
    }

    public async Task<BookDetails?> GetBookAsync(string isbn, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.GetAsync($"/api/books/{Uri.EscapeDataString(isbn)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookDetails>(ct);
    }

    public async Task<IReadOnlyList<InstitutionView>> GetInstitutionsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<InstitutionView>>("/api/institutions", ct) ?? [];
}
