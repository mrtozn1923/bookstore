using AwesomeAssertions.Extensions;
using Bookstore.Catalog;

namespace Bookstore.Ordering.AwesomeAssertions.Tests;

public sealed class CampaignTests
{
    [Fact]
    public void Campaign_ends_one_week_after_it_starts()
    {
        Campaign campaign = Campaign.StartNow("AUTUMN26", 0.15m, TimeSpan.FromDays(7), TimeProvider.System);

        campaign.EndsAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), 2.Seconds());
        campaign.EndsAtUtc.Should().BeExactly(campaign.StartsAtUtc + 7.Days());
    }

    [Fact]
    public void Discount_above_half_price_is_rejected()
    {
        Action act = () => Campaign.StartNow("TOO-MUCH", 0.60m, TimeSpan.FromDays(1), TimeProvider.System);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("discountRate");
    }
}
