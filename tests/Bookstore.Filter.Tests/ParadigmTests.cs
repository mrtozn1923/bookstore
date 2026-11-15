using Bookstore.Catalog;
using Bookstore.Paradigms;

namespace Bookstore.Filter.Tests;

public class ParadigmTests
{
    [Theory]
    [InlineData("Jane Austen", 2998.00)]
    [InlineData("J.R.R. Tolkien", 0)]
    [InlineData("Nobody", 0)]
    public void Three_styles_compute_the_same_stock_value(string author, double expected)
    {
        decimal value = (decimal)expected;

        Assert.Equal(value, StockValue.Imperative(SampleBooks.All, author));
        Assert.Equal(value, StockValue.Declarative(SampleBooks.All, author));
        Assert.Equal(value, StockValue.Matched(SampleBooks.All, author));
    }
}
