using Microsoft.Extensions.Options;

namespace Bookstore.Web.Catalog;

public sealed class InMemoryBookCatalog(IOptionsMonitor<CatalogOptions> options) : IBookCatalog
{
    public Task<IReadOnlyList<Book>> ListAsync(string? category, CancellationToken cancellationToken)
    {
        ThrowIfOutage();
        IReadOnlyList<Book> books = string.IsNullOrEmpty(category)
            ? CatalogData.Books
            : CatalogData.Books.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(books);
    }

    public Task<Book?> FindAsync(int id, CancellationToken cancellationToken)
    {
        ThrowIfOutage();
        return Task.FromResult(CatalogData.Books.FirstOrDefault(b => b.Id == id));
    }

    public Task<IReadOnlyList<Book>> SuggestAsync(string? hint, int count, CancellationToken cancellationToken)
    {
        ThrowIfOutage();
        string[] words = (hint ?? "").Split(['-', '/', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4)
            .ToArray();
        List<Book> matches = CatalogData.Books
            .Where(b => words.Any(w => b.Title.Contains(w, StringComparison.OrdinalIgnoreCase)
                                    || b.Author.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .Take(count)
            .ToList();
        // No match: suggest the books with the most copies in stock
        IReadOnlyList<Book> result = matches.Count > 0
            ? matches
            : CatalogData.Books.Where(b => b.Stock > 0).OrderByDescending(b => b.Stock).Take(count).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<string>> CategoriesAsync(CancellationToken cancellationToken)
    {
        ThrowIfOutage();
        IReadOnlyList<string> categories = CatalogData.Books.Select(b => b.Category).Distinct().Order().ToList();
        return Task.FromResult(categories);
    }

    private void ThrowIfOutage()
    {
        if (options.CurrentValue.SimulateOutage)
        {
            throw new TimeoutException("Catalog store did not answer within 5 seconds.");
        }
    }
}
