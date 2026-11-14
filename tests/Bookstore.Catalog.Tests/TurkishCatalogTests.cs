using System.Globalization;

namespace Bookstore.Catalog.Tests;

public sealed class TurkishCatalogTests
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    [Fact]
    public void Every_turkish_author_is_found_by_a_lowercase_search()
    {
        List<Book> catalog = BookFaker.Create(seed: 2026, locale: "tr").Generate(1_000);
        BookSearch search = new(catalog);

        Assert.All(catalog.Select(b => b.Author).Distinct(), author =>
            Assert.NotEmpty(search.ByAuthor(author.ToLower(Turkish))));
    }

    [Fact]
    public void English_authors_are_still_found_in_a_mixed_catalog()
    {
        List<Book> catalog = [.. BookFaker.Create(seed: 2026, locale: "tr").Generate(500), .. BookFaker.Create(seed: 7).Generate(500)];
        BookSearch search = new(catalog);

        Assert.All(catalog.Where(b => b.Language == "en").Select(b => b.Author).Distinct(), author =>
            Assert.NotEmpty(search.ByAuthor(author.ToLowerInvariant())));
    }
}
