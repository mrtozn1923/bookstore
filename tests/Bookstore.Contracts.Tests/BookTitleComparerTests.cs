using Bookstore.Contracts;

namespace Bookstore.Contracts.Tests;

public sealed class BookTitleComparerTests
{
    [Fact]
    public void Sorts_volume_numbers_numerically()
    {
        string[] titles = ["Volume 10", "The Hobbit", "Volume 2", "volume 1"];

        string[] sorted = [.. titles.Order(BookTitleComparer.Instance)];

        Assert.Equal(["The Hobbit", "volume 1", "Volume 2", "Volume 10"], sorted);
    }
}
