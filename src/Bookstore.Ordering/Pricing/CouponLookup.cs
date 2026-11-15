using Bookstore.Ordering.Functional;

namespace Bookstore.Ordering.Pricing;

public static class CouponLookup
{
    public static Option<string> NormalizeCode(string? code) =>
        code.ToOption()
            .Map(text => text.Trim().ToUpperInvariant())
            .Where(text => text.Length > 0);

    public static Option<Campaign> ActiveCampaign(
        string? code, IReadOnlyDictionary<string, Campaign> campaigns, DateTimeOffset now) =>
        NormalizeCode(code)
            .Bind(normalized => campaigns.Find(normalized))
            .Where(campaign => campaign.IsActiveAt(now));

    public static decimal CouponDiscount(
        decimal subtotal, string? code, IReadOnlyDictionary<string, Campaign> campaigns, DateTimeOffset now) =>
        ActiveCampaign(code, campaigns, now)
            .Map(campaign => Math.Round(subtotal * campaign.Rate, 2, MidpointRounding.AwayFromZero))
            .GetValueOrDefault(0m);

    // The same chain in LINQ query syntax
    public static Option<decimal> CouponDiscountQuery(
        decimal subtotal, string? code, IReadOnlyDictionary<string, Campaign> campaigns, DateTimeOffset now) =>
        from normalized in NormalizeCode(code)
        from campaign in campaigns.Find(normalized)
        where campaign.IsActiveAt(now)
        select Math.Round(subtotal * campaign.Rate, 2, MidpointRounding.AwayFromZero);
}
