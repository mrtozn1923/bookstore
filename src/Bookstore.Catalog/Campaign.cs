namespace Bookstore.Catalog;

public sealed record Campaign(string Code, decimal DiscountRate, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc)
{
    public static Campaign StartNow(string code, decimal discountRate, TimeSpan duration, TimeProvider clock)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(discountRate, 0.5m);

        DateTimeOffset now = clock.GetUtcNow();
        return new Campaign(code, discountRate, now, now + duration);
    }

    public bool IsActive(DateTimeOffset at) => at >= StartsAtUtc && at < EndsAtUtc;
}
