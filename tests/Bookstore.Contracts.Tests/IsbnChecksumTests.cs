using Bookstore.Contracts;
using Xunit;

namespace Bookstore.Contracts.Tests;

public sealed class IsbnChecksumTests
{
    [Theory]
    [InlineData("978044117271", 9)]
    [InlineData("978026110357", 3)]
    public void Compute_returns_the_check_digit(string firstTwelve, int expected)
    {
        Assert.Equal(expected, IsbnChecksum.Compute(firstTwelve));
    }
}
