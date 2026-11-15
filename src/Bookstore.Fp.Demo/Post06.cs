using System.Text.Json;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Fp.Demo;

public static class Post06
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    private static readonly Dictionary<string, Campaign> Campaigns = new()
    {
        ["AUTUMN10"] = new("AUTUMN10", 0.10m, new(2026, 11, 1, 0, 0, 0, Istanbul), new(2026, 12, 1, 0, 0, 0, Istanbul)),
        ["SUMMER15"] = new("SUMMER15", 0.15m, new(2026, 6, 1, 0, 0, 0, Istanbul), new(2026, 9, 1, 0, 0, 0, Istanbul))
    };

    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, Istanbul);

    public static void Guess()
    {
        // VIP: the rate isn't decided yet
        Dictionary<string, decimal?> rates = new() { ["AUTUMN10"] = 0.10m, ["VIP"] = null };

        decimal? vip = rates.GetValueOrDefault("VIP");
        decimal? spring = rates.GetValueOrDefault("SPRING5");
        Console.WriteLine($"VIP: {vip?.ToString() ?? "null"}, SPRING5: {spring?.ToString() ?? "null"}, same: {vip == spring}");

        decimal[] lineDiscounts = [];
        Console.WriteLine($"first discount: {lineDiscounts.FirstOrDefault()}");
    }

    public static void Basics()
    {
        Option<decimal> some = Option.Some(0.10m);
        Option<decimal> none = Option<decimal>.None;
        Option<decimal> fromDefault = default;

        Console.WriteLine($"{some}, {none}, default: {fromDefault}, equal: {none == fromDefault}");
        Console.WriteLine(some.Match(rate => $"rate {rate}", () => "no rate"));
        Console.WriteLine(none.Match(rate => $"rate {rate}", () => "no rate"));
        Console.WriteLine($"map: {some.Map(rate => rate * 100)}, {none.Map(rate => rate * 100)}");
    }

    public static void Chain()
    {
        string?[] codes = ["AUTUMN10", " autumn10 ", "SUMMER15", "SPRING5", "", null];
        foreach (string? code in codes)
        {
            Option<Campaign> campaign = CouponLookup.ActiveCampaign(code, Campaigns, Now);
            decimal discount = CouponLookup.CouponDiscount(249.40m, code, Campaigns, Now);
            Console.WriteLine($"{Quote(code),-14} campaign: {campaign.Map(c => c.Code),-16} discount: {discount}");
        }
    }

    public static void Query()
    {
        string?[] codes = [" autumn10 ", "SUMMER15", null];
        foreach (string? code in codes)
        {
            Option<decimal> query = CouponLookup.CouponDiscountQuery(249.40m, code, Campaigns, Now);
            decimal method = CouponLookup.CouponDiscount(249.40m, code, Campaigns, Now);
            Console.WriteLine($"{Quote(code),-14} query: {query,-12} method: {method}");
        }
    }

    public static void Nested()
    {
        // Dune is in the catalog but its price isn't set yet
        Dictionary<string, Option<decimal>> listPrices = new()
        {
            ["9780141439587"] = Option.Some(59.90m),
            ["9780441013593"] = Option<decimal>.None
        };

        string[] isbns = ["9780141439587", "9780441013593", "9780000000000"];
        foreach (string isbn in isbns)
        {
            Option<Option<decimal>> nested = listPrices.Find(isbn);
            Option<decimal> flat = listPrices.Find(isbn).Bind(price => price);
            string meaning = nested.Match(
                price => price.Match(value => $"price {value}", () => "in catalog, no price yet"),
                () => "not in catalog");
            Console.WriteLine($"{isbn}: {nested,-15} {flat,-12} {meaning}");
        }
    }

    public static async Task Shapes()
    {
        IEnumerable<string> isbns =
            from order in new[] { new[] { "9780441013593", "9780141439587" }, new[] { "9780261103344" } }
            from isbn in order
            select isbn;
        Console.WriteLine($"IEnumerable: {string.Join(", ", isbns)}");

        Option<decimal> rate =
            from code in CouponLookup.NormalizeCode("autumn10")
            from campaign in Campaigns.Find(code)
            select campaign.Rate;
        Console.WriteLine($"Option: {rate}");

        Console.WriteLine($"Task: {await AfterMemberDiscountAsync()}");
    }

    // await unwraps the Task the way Bind unwraps an Option
    private static async Task<decimal> AfterMemberDiscountAsync() => await LoadSubtotalAsync() - 15m;

    private static Task<decimal> LoadSubtotalAsync() => Task.FromResult(249.40m);

    public static void Pitfalls()
    {
        string json = JsonSerializer.Serialize(Option.Some(0.10m), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Console.WriteLine($"json: {json}");

        decimal? rate = null;
        try
        {
#pragma warning disable CS8629 // NRT warns here; disabled for the demo
            Console.WriteLine(rate.Value);
#pragma warning restore CS8629
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string Quote(string? code) => code is null ? "null" : $"\"{code}\"";
}
