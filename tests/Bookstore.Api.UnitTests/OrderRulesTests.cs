using Bookstore.Api.Endpoints;

namespace Bookstore.Api.UnitTests;

public sealed class OrderRulesTests
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void Order_can_be_cancelled_only_within_30_minutes(int minutesLater, bool expected)
    {
        Assert.Equal(expected, OrderRules.CanCancel(PlacedAt, PlacedAt.AddMinutes(minutesLater)));
    }
}
