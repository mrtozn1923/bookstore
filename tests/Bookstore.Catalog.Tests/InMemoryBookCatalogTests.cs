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
}
