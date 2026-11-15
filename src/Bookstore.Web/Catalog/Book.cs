using System.Text.RegularExpressions;

namespace Bookstore.Web.Catalog;

public sealed partial record Book(
    int Id, string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear, int Stock)
{
    public string Slug => NonSlugChars().Replace(Title.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();
}
