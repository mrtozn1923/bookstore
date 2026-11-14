using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Bookstore.Api.Tests;

public class FlashSaleServiceTests
{
    [Fact]
    public void IsActive_FollowsTheClock()
    {
        // Test: Microsoft.Extensions.TimeProvider.Testing package
        var time = new FakeTimeProvider(new DateTimeOffset(2024, 11, 29, 20, 30, 0, TimeSpan.Zero));
        var sale = new FlashSaleService(time);
        Assert.True(sale.IsActive());

        time.Advance(TimeSpan.FromHours(2));
        Assert.False(sale.IsActive());
    }
}
