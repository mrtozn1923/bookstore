using Bookstore.Catalog;
using Bookstore.Filter.Evaluation;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Tests;

public class InterpreterTests
{
    private static string[] Titles(string filter) =>
        [.. Interpreter.Filter(SampleBooks.All, Parser.Parse(filter).Root).Select(b => b.Title)];

    [Fact]
    public void Filter_PriceAndAuthor()
    {
        Assert.Equal(["Emma", "Persuasion", "Sense and Sensibility"], Titles("price < 300 and author = \"Jane Austen\""));
    }

    [Fact]
    public void Filter_NotAndContains()
    {
        Assert.Equal(["Emma", "Persuasion"], Titles("author contains \"Austen\" and not (price > 200)"));
    }

    [Fact]
    public void Filter_MatchesTheSameBooksAsLinq()
    {
        IEnumerable<Book> expected = SampleBooks.All.Where(b => b.Stock == 0 || (b.Price < 300m && b.Year < 1900));
        Assert.Equal(expected.Select(b => b.Title), Titles("stock = 0 or price < 300 and year < 1900"));
    }

    [Fact]
    public void Filter_TypeErrorSurfacesOnlyForTheBookThatReachesIt()
    {
        Expr filter = Parser.Parse("stock > 0 or price = \"abc\"").Root;
        List<string> seen = [];

        FilterRuntimeException ex = Assert.Throws<FilterRuntimeException>(() =>
        {
            foreach (Book book in Interpreter.Filter(SampleBooks.All, filter))
                seen.Add(book.Title);
        });

        Assert.Equal(["Dune"], seen);
        Assert.Equal("operator '=' at 1:20 cannot compare number with string", ex.Message);
    }

    [Fact]
    public void Filter_EmptyCatalog_HidesTheTypeError()
    {
        Assert.Empty(Interpreter.Filter([], Parser.Parse("price = \"abc\"").Root));
    }
}
