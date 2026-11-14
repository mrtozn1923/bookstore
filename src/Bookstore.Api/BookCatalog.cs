using System.Globalization;
using Microsoft.Extensions.Options;

namespace Bookstore.Api;

public sealed record Book(string Isbn, string Title, string Author, decimal PriceTry);

public sealed record BookView(string Isbn, string Title, string ShelfLabel, decimal PriceTry);

public sealed class BookCatalog(IBookSource source, IOptions<StoreOptions> options)
{
    public async Task<IReadOnlyList<BookView>> SortedByTitleAsync(CancellationToken cancellationToken)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(options.Value.Culture);
        StringComparer comparer = StringComparer.Create(culture, ignoreCase: false);
        IReadOnlyList<Book> books = await source.GetAllAsync(cancellationToken);

        return [.. books
            .OrderBy(b => b.Title, comparer)
            .Select(b => new BookView(b.Isbn, b.Title, b.Title.ToUpper(culture), b.PriceTry))];
    }
}
