using Bookstore.Contracts;

namespace Bookstore.Contracts.Tests;

public sealed class IsbnTests
{
    [Theory]
    [InlineData("978-0-441-17271-9")]
    [InlineData("9780261103573")]
    public void TryParse_accepts_valid_isbn13(string input)
    {
        Assert.True(Isbn.TryParse(input, out Isbn isbn));
        Assert.Equal(13, isbn.Value.Length);
    }

    [Theory]
    [InlineData("978-0-441-17271-0")]
    [InlineData("0441172717")]
    [InlineData("")]
    public void TryParse_rejects_invalid_input(string input)
    {
        Assert.False(Isbn.TryParse(input, out _));
    }
}
