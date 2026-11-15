namespace Bookstore.Ordering.Tests;

public sealed class InstitutionalPricingTests
{
    private const string Dune = "9780441172719";
    private const string Emma = "9780141439587";
    private const string TheHobbit = "9780547928227";
    private const string Middlemarch = "9780141439549";

    // TC-01..TC-04: the 20-item minimum applies to the order total
    public static TheoryData<int[], bool> MinimumQuantityCases => new()
    {
        { [20], true },
        { [19], false },
        { [19, 1], true },
        { [12, 7], false },
    };

    [Theory]
    [MemberData(nameof(MinimumQuantityCases))]
    public void Minimum_quantity_applies_to_the_order_total(int[] quantities, bool accepted)
    {
        OrderLine[] lines = [.. quantities.Select(quantity => new OrderLine(Dune, 250m, quantity))];

        PricingResult result = InstitutionalPricing.Price(lines);

        Assert.Equal(accepted, result is PricingResult.Priced);
    }

    // TC-05: a rejection states the order's total quantity
    [Fact]
    public void Rejection_states_the_order_total()
    {
        PricingResult result = InstitutionalPricing.Price(
            [new OrderLine(Dune, 250m, 12), new OrderLine(Emma, 84.95m, 7)]);

        PricingResult.Rejected rejected = Assert.IsType<PricingResult.Rejected>(result);
        Assert.Contains("this order has 19", rejected.Reason);
    }

    // TC-06, TC-07: discounts don't stack, the larger one wins
    public static TheoryData<string, decimal, decimal, int, decimal, decimal> LargerDiscountWinsCases => new()
    {
        { TheHobbit, 120m, 0.25m, 20, 0.25m, 1800.00m },
        { Middlemarch, 200m, 0.05m, 20, 0.10m, 3600.00m },
    };

    [Theory]
    [MemberData(nameof(LargerDiscountWinsCases))]
    public void Larger_discount_wins(
        string isbn, decimal listPrice, decimal campaignRate, int quantity, decimal expectedRate, decimal expectedTotal)
    {
        PricingResult result = InstitutionalPricing.Price([new OrderLine(isbn, listPrice, quantity, campaignRate)]);

        PricedLine line = Assert.Single(Assert.IsType<PricingResult.Priced>(result).Lines);
        Assert.Equal(expectedRate, line.AppliedRate);
        Assert.Equal(expectedTotal, line.Total);
    }

    // TC-08: each line is rounded separately, half a kurus rounds up
    [Fact]
    public void Each_line_is_rounded_half_up()
    {
        PricingResult result = InstitutionalPricing.Price(
            [new OrderLine(Emma, 84.95m, 3), new OrderLine(Dune, 250m, 17)]);

        PricingResult.Priced priced = Assert.IsType<PricingResult.Priced>(result);
        Assert.Equal([229.37m, 3825.00m], priced.Lines.Select(line => line.Total));
        Assert.Equal(4054.37m, priced.Total);
    }
}
