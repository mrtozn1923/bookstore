namespace Bookstore.Catalog;

public sealed record Campaign(string Code, decimal DiscountRate, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc)
{
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromDays(90);

    public static Campaign StartNow(string code, decimal discountRate, TimeSpan duration, TimeProvider clock)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(discountRate, 0.5m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(duration, MaximumDuration);

        DateTimeOffset now = clock.GetUtcNow();
        return new Campaign(code, discountRate, now, now + duration);
    }

    public bool IsActive(DateTimeOffset at) => at >= StartsAtUtc && at < EndsAtUtc;

    public decimal Apply(decimal price) => Math.Round(price * (1 - DiscountRate), 2, MidpointRounding.AwayFromZero);
}
