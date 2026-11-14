using Bookstore.Api.Catalog;

namespace Bookstore.Api.Tests;

public class BookCatalogTests
{
    private readonly BookCatalog _catalog = new();

    [Fact]
    public void Find_returns_book_by_isbn() =>
        Assert.Equal("Dune", _catalog.Find("9780441172719")?.Title);

    [Fact]
    public void Search_is_case_insensitive() =>
        Assert.Equal(2, _catalog.Search("austen").Count);

    [Fact]
    public void Every_book_has_a_valid_isbn() =>
        Assert.All(_catalog.All(), b => Assert.True(Isbn.IsValid(b.Isbn)));
}
