namespace Bookstore.Catalog;

public sealed record Campaign(string Code, decimal Rate, DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static Campaign ForDays(string code, decimal rate, DateOnly firstDay, DateOnly lastDay) =>
        new(code, rate, StartOfDayInIstanbul(firstDay), StartOfDayInIstanbul(lastDay.AddDays(1)));

    // [StartsAt, EndsAt): the end instant is not part of the campaign
    public bool IsActiveAt(DateTimeOffset instant) => instant >= StartsAt && instant < EndsAt;

    private static DateTimeOffset StartOfDayInIstanbul(DateOnly day)
    {
        DateTime local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Istanbul.GetUtcOffset(local));
    }
}

public sealed class CampaignCalendar(TimeProvider time)
{
    public IReadOnlyList<Campaign> ActiveCampaigns(IEnumerable<Campaign> campaigns)
    {
        DateTimeOffset now = time.GetUtcNow();
        return [.. campaigns.Where(campaign => campaign.IsActiveAt(now))];
    }
}
