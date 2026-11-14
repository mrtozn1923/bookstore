using Bookstore.Tools;
using Xunit;

namespace Bookstore.Tools.Tests;

public sealed class BookRowTests
{
    [Theory]
    [InlineData("9780441172719", true)]
    [InlineData("9780141439587", true)]
    [InlineData("9780141439510", false)]
    [InlineData("978014143958", false)]
    [InlineData("97801414395AB", false)]
    public void HasValidIsbn13_checks_length_digits_and_check_digit(string isbn, bool expected)
    {
        BookRow row = new(isbn, "Any", "Any", 100m);

        Assert.Equal(expected, row.HasValidIsbn13());
    }

    [Fact]
    public void Parse_trims_fields_and_reads_invariant_price()
    {
        BookRow row = BookRow.Parse("9780441172719, Dune ,Frank Herbert,420.50");

        Assert.Equal("Dune", row.Title);
        Assert.Equal(420.50m, row.PriceTry);
    }
}
