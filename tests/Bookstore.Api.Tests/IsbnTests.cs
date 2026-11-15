using Bookstore.Catalog;

namespace Bookstore.Api.Tests;

public sealed class IsbnTests
{
    [Theory]
    [InlineData("9780441172719", "0441172717")]   // Dune
    [InlineData("9780141439587", "0141439580")]   // Emma
    [InlineData("9780804429573", "080442957X")]   // check digit 10 is written as X
    public void Isbn13_with_978_prefix_converts_to_isbn10(string isbn13, string expected)
    {
        Assert.Equal(expected, Isbn.ToIsbn10(isbn13));
    }

    [Theory]
    [InlineData("9791032305690")]   // 979 prefix: no ISBN-10 exists
    [InlineData("9780441172718")]   // wrong check digit
    [InlineData("978044117271")]    // 12 digits
    [InlineData("97804411727X9")]
    public void Other_values_have_no_isbn10(string isbn13)
    {
        Assert.Null(Isbn.ToIsbn10(isbn13));
    }
}
