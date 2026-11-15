using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Catalog.Tests;

public sealed class CampaignTests
{
    // Campaign 1-30 November in Istanbul time; interval [1 Nov 00:00, 1 Dec 00:00)
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 0, 0, 0, Istanbul);
    private static readonly DateTimeOffset End = new(2026, 12, 1, 0, 0, 0, Istanbul);
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);

    private static readonly Campaign NovemberSale =
        Campaign.ForDays("NOVEMBER25", 0.25m, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

    // Boundary values and their neighbors (smallest step: 1 tick = 100 ns)
    public static TheoryData<DateTimeOffset, bool> Boundaries => new()
    {
        { Start - 2 * Tick, false },
        { Start - Tick, false },
        { Start, true },
        { Start + Tick, true },
        { End - 2 * Tick, true },
        { End - Tick, true },
        { End, false },
        { End + Tick, false },
    };

    [Theory]
    [MemberData(nameof(Boundaries))]
    public void Campaign_runs_from_first_day_to_end_of_last_day_in_istanbul(DateTimeOffset instant, bool active)
    {
        Assert.Equal(active, NovemberSale.IsActiveAt(instant));
    }

    [Fact]
    public void Calendar_drops_the_campaign_at_midnight_istanbul_time()
    {
        FakeTimeProvider time = new(End - Tick);
        CampaignCalendar calendar = new(time);
        Assert.Single(calendar.ActiveCampaigns([NovemberSale]));

        time.Advance(Tick);

        Assert.Empty(calendar.ActiveCampaigns([NovemberSale]));
    }
}
