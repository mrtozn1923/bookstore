namespace Bookstore.Catalog.Tests;

public sealed class InMemoryBookCatalogTests
{
    [Fact]
    public void FindByIsbn_UnknownIsbn_ReturnsNull()
    {
        InMemoryBookCatalog catalog = new();

        Book? book = catalog.FindByIsbn("9780000000000");

        Assert.Null(book);
    }

    [Fact]
    public void GetAll_SeededBooks_ReturnsOnlyThoseBooks()
    {
        InMemoryBookCatalog catalog = new([new("9780000000019", "Dune", "Frank Herbert", 350.90m)]);

        IReadOnlyList<Book> books = catalog.GetAll();

        Assert.Equal("9780000000019", Assert.Single(books).Isbn);
    }
}
