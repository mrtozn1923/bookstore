namespace Bookstore.Ordering.Pricing;

public sealed record Campaign(string Code, decimal Rate, DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    public bool IsActiveAt(DateTimeOffset now) => now >= StartsAt && now < EndsAt;
}
