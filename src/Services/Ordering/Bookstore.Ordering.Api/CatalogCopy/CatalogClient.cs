namespace Bookstore.Ordering.CatalogCopy;

// Only the fields Ordering needs; unknown fields in Catalog's response are ignored (tolerant reader).
public sealed record CatalogBookDto(string Isbn, string Title, decimal Price, string Currency, int Version);

public sealed record CatalogPage(IReadOnlyList<CatalogBookDto> Items, int Page, int PageSize, int TotalCount);

public sealed class CatalogClient(HttpClient http)
{
    public async IAsyncEnumerable<CatalogBookDto> GetAllBooksAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        const int pageSize = 50;
        for (int page = 1; ; page++)
        {
            CatalogPage? result = await http.GetFromJsonAsync<CatalogPage>(
                $"/api/v1/books?page={page}&pageSize={pageSize}", ct);
            if (result is null || result.Items.Count == 0)
            {
                yield break;
            }
            foreach (CatalogBookDto book in result.Items)
            {
                yield return book;
            }
            if (page * pageSize >= result.TotalCount)
            {
                yield break;
            }
        }
    }
}
