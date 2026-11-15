using Bookstore.Catalog;
using Bookstore.JitLab;

namespace Bookstore.Filter.Tests;

// The JIT may rewrite these methods (cloned loops, devirtualized calls, stack-allocated arrays); the results must not change.
public class JitLabTests
{
    private static readonly Book[] Books = [.. SampleBooks.All];

    [Fact]
    public void Both_loop_shapes_count_the_same_books()
    {
        Assert.Equal(5, Lab.CountInStock(Books));
        Assert.Equal(Lab.CountInStock(Books), Lab.CountInStock(Books, Books.Length));
        Assert.Equal(2, Lab.CountInStock(Books, 3));
    }

    [Fact]
    public void Take_beyond_the_array_still_throws()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Lab.CountInStock(Books, Books.Length + 1));
    }

    [Fact]
    public void Interface_rules_and_the_enumerable_loop_agree()
    {
        Assert.Equal(Lab.CountInStock(Books), Lab.CountMatches(Books, new InStock()));
        Assert.Equal(Lab.CountInStock(Books), Lab.CountInStockSeq(Books));
        Assert.Equal(4, Lab.CountMatches(Books, new PublishedBefore(1900)));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(7, 3)]
    [InlineData(50, 4)]
    public void Shipping_days_grow_with_quantity(int quantity, int days)
    {
        Assert.Equal(days, Lab.ShippingDays(quantity));
    }

    [Fact]
    public void Captured_limit_filters_by_price()
    {
        Assert.Equal(3, Lab.CountCheaperThan(Books, 250m));
    }
}
