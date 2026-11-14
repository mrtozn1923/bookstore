using Bookstore.Api.Catalog;

namespace Bookstore.Api.Tests;

public class IsbnTests
{
    [Theory]
    [InlineData("9780441172719")]
    [InlineData("978-0-547-92822-7")]
    public void Valid_isbn13_passes(string value) => Assert.True(Isbn.IsValid(value));

    [Fact]
    public void Wrong_check_digit_fails() => Assert.False(Isbn.IsValid("9780441172710"));

    [Fact]
    public void Wrong_length_fails() => Assert.False(Isbn.IsValid("978044117271"));
}
