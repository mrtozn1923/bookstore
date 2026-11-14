using System.Globalization;

namespace Bookstore.Catalog;

public sealed class BookSearch(IEnumerable<Book> books)
{
    private readonly IReadOnlyList<Book> books = [.. books];

    // Case rules depend on the book's language: "İdil" matches "idil" only under Turkish rules
    public IReadOnlyList<Book> ByAuthor(string term) =>
    [
        .. books
            .Where(b => CultureInfo.GetCultureInfo(b.Language).CompareInfo
                .IndexOf(b.Author, term, CompareOptions.IgnoreCase) >= 0)
            .OrderBy(b => b.Title),
    ];
}
