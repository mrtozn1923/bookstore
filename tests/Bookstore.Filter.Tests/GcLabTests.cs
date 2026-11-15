using Bookstore.Catalog;
using Bookstore.GcLab;

namespace Bookstore.Filter.Tests;

public class GcLabTests
{
    [Fact]
    public void Generated_catalog_cycles_the_sample_books_with_varying_prices()
    {
        Book[] catalog = LabCatalog.Generate(14);

        Assert.Equal(14, catalog.Length);
        Assert.Equal("Dune", catalog[6].Title);
        Assert.Equal(SampleBooks.All[0].Price + 6, catalog[6].Price);
        Assert.All(catalog, book => Assert.InRange(book.Stock, 0, 6));
    }

    [Fact]
    public void Shelf_keeps_the_latest_book_and_counts()
    {
        Shelf shelf = new();
        foreach (Book book in SampleBooks.All)
            shelf.Put(book);

        Assert.Equal(6, shelf.Count);
        Assert.Same(SampleBooks.All[^1], shelf.Latest);
    }
}
