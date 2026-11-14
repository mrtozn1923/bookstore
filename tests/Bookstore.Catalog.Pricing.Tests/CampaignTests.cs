using Bookstore.Catalog;

namespace Bookstore.Catalog.Pricing.Tests;

[TestClass]
public sealed class CampaignTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void Discount_above_half_is_rejected()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Campaign.StartNow("BLACKFRIDAY", 0.60m, TimeSpan.FromDays(3), Clock));

        Assert.AreEqual("discountRate", ex.ParamName);
    }

    [TestMethod]
    public void Campaign_longer_than_90_days_is_rejected()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Campaign.StartNow("SUMMER", 0.20m, TimeSpan.FromDays(120), Clock));

        Assert.AreEqual("duration", ex.ParamName);
    }

    // Attribute arguments cannot be decimal: prices in kurus, rates in percent
    [TestMethod]
    [DataRow(1250, 10, 1125)]
    [DataRow(990, 15, 842)]
    [DataRow(5, 50, 3)]
    public void Discount_is_rounded_away_from_zero(int priceKurus, int ratePercent, int expectedKurus)
    {
        Campaign campaign = Campaign.StartNow("AUTUMN26", ratePercent / 100m, TimeSpan.FromDays(7), Clock);

        Assert.AreEqual(expectedKurus / 100m, campaign.Apply(priceKurus / 100m));
    }

    public static IEnumerable<(DateTimeOffset At, bool Active)> BlackFridayMoments =>
    [
        (new DateTimeOffset(2026, 11, 26, 23, 59, 59, TimeSpan.Zero), false),
        (new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero), true),
        (new DateTimeOffset(2026, 11, 29, 23, 59, 59, TimeSpan.Zero), true),
        (new DateTimeOffset(2026, 11, 30, 0, 0, 0, TimeSpan.Zero), false),
    ];

    [TestMethod]
    [DynamicData(nameof(BlackFridayMoments))]
    public void Campaign_is_active_from_start_until_just_before_the_end(DateTimeOffset at, bool active)
    {
        Campaign blackFriday = Campaign.StartNow("BLACKFRIDAY", 0.30m, TimeSpan.FromDays(3), Clock);

        Assert.AreEqual(active, blackFriday.IsActive(at));
    }
}
