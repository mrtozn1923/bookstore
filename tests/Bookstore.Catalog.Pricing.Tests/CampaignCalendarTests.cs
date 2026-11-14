using Bookstore.Catalog;

namespace Bookstore.Catalog.Pricing.Tests;

[TestClass]
public sealed class CampaignCalendarTests(TestContext testContext)
{
    private static IReadOnlyList<Campaign> calendar = [];

    // Static and takes a TestContext: runs once before the first test of this class
    [ClassInitialize]
    public static void LoadCalendar(TestContext context)
    {
        FixedClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        calendar =
        [
            Campaign.StartNow("WINTER26", 0.20m, TimeSpan.FromDays(60), clock),
            Campaign.StartNow("READING-WEEK", 0.10m, TimeSpan.FromDays(7), clock),
            Campaign.StartNow("BOOKFAIR", 0.25m, TimeSpan.FromDays(10), clock),
        ];
        context.WriteLine($"Calendar loaded with {calendar.Count} campaigns");
    }

    [TestMethod]
    public void Calendar_codes_follow_the_naming_rule()
    {
        Assert.HasCount(3, calendar);
        Assert.ContainsSingle(c => c.Code == "BOOKFAIR", calendar);
        foreach (Campaign campaign in calendar)
        {
            Assert.MatchesRegex("^[A-Z0-9-]+$", campaign.Code);
        }
    }

    [TestMethod]
    public void Discounts_stay_within_the_allowed_range()
    {
        foreach (Campaign campaign in calendar)
        {
            Assert.IsInRange(0.05m, 0.50m, campaign.DiscountRate);
        }
    }

    [TestMethod]
    public async Task Test_context_carries_the_name_and_the_cancellation_token()
    {
        await Task.Delay(10, testContext.CancellationToken);

        Assert.AreEqual(nameof(Test_context_carries_the_name_and_the_cancellation_token), testContext.TestName);
        Assert.StartsWith("Bookstore.Catalog.Pricing", testContext.FullyQualifiedTestClassName!);
    }
}
