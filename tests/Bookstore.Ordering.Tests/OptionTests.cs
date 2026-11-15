using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Ordering.Tests;

public class OptionTests
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    private static readonly IReadOnlyDictionary<string, Campaign> Campaigns = new Dictionary<string, Campaign>
    {
        ["AUTUMN10"] = new("AUTUMN10", 0.10m, new(2026, 11, 1, 0, 0, 0, Istanbul), new(2026, 12, 1, 0, 0, 0, Istanbul))
    };

    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, Istanbul);

    [Fact]
    public void Default_is_none() => Assert.Equal(Option<int>.None, default);

    [Fact]
    public void Map_and_bind_skip_the_function_on_none()
    {
        int calls = 0;
        Option<int> none = Option<int>.None;

        Option<int> mapped = none.Map(x => { calls++; return x + 1; });
        Option<int> bound = none.Bind(x => { calls++; return Option.Some(x + 1); });

        Assert.Equal(Option<int>.None, mapped);
        Assert.Equal(Option<int>.None, bound);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("AUTUMN10", 24.94)]
    [InlineData(" autumn10 ", 24.94)]
    [InlineData("SPRING5", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void Coupon_discount_needs_a_known_active_code(string? code, decimal expected) =>
        Assert.Equal(expected, CouponLookup.CouponDiscount(249.40m, code, Campaigns, Now));

    [Theory]
    [InlineData("AUTUMN10")]
    [InlineData("SPRING5")]
    [InlineData(null)]
    public void Query_syntax_matches_the_method_chain(string? code) =>
        Assert.Equal(
            CouponLookup.CouponDiscount(249.40m, code, Campaigns, Now),
            CouponLookup.CouponDiscountQuery(249.40m, code, Campaigns, Now).GetValueOrDefault(0m));

    [Fact]
    public void Campaign_outside_its_window_is_none() =>
        Assert.Equal(Option<Campaign>.None, CouponLookup.ActiveCampaign("AUTUMN10", Campaigns, new(2026, 12, 1, 0, 0, 0, Istanbul)));
}
