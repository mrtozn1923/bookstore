using AutoFixture;

namespace Bookstore.Catalog.Tests;

public sealed class AutoFixtureTests
{
    private readonly Fixture fixture = new();

    public AutoFixtureTests() => fixture.Customize(new BookstoreCustomization());

    [Fact]
    public void Create_fills_every_property()
    {
        Book book = fixture.Create<Book>();

        Assert.True(Isbn13.IsValid(book.Isbn));
        Assert.StartsWith("Title", book.Title);
        Assert.NotNull(book.Subtitle);
    }

    [Fact]
    public void Build_sets_only_what_the_test_cares_about()
    {
        Book dune = fixture.Build<Book>()
            .With(b => b.Title, "Dune")
            .With(b => b.Author, "Frank Herbert")
            .With(b => b.Language, "en")
            .Without(b => b.Subtitle)
            .Create();
        BookSearch search = new([dune, .. fixture.CreateMany<Book>(20)]);

        Assert.Equal([dune], search.ByAuthor("herbert"));
    }

    [Theory, BookstoreAutoData]
    public void Search_by_author_ignores_case(Book book, List<Book> others)
    {
        BookSearch search = new([.. others, book]);

        Assert.Equal([book], search.ByAuthor(book.Author.ToUpperInvariant()));
    }
}
