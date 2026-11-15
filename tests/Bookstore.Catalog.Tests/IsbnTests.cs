using Bookstore.Catalog;

namespace Bookstore.Catalog.Tests;

public class IsbnTests
{
    [Theory]
    [InlineData("9780441013593")]
    [InlineData("978-0-441-01359-3")]
    [InlineData("978 0 441 01359 3")]
    public void Parse_normalizes_to_13_digits(string text) =>
        Assert.Equal("9780441013593", Isbn.Parse(text).Value);

    [Theory]
    [InlineData("9780441013594")]
    [InlineData("978044101359")]
    [InlineData("97804410135X3")]
    [InlineData("")]
    public void TryParse_rejects_invalid_text(string text) =>
        Assert.False(Isbn.TryParse(text, out _));

    [Fact]
    public void Two_isbns_with_the_same_digits_are_equal() =>
        Assert.Equal(Isbn.Parse("978-0-441-01359-3"), Isbn.Parse("9780441013593"));
}
