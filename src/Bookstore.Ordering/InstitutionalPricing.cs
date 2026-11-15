namespace Bookstore.Ordering;

public sealed record OrderLine(string Isbn, decimal ListPrice, int Quantity, decimal CampaignRate = 0m);

public sealed record PricedLine(string Isbn, int Quantity, decimal AppliedRate, decimal Total);

public abstract record PricingResult
{
    public sealed record Priced(IReadOnlyList<PricedLine> Lines, decimal Total) : PricingResult;

    public sealed record Rejected(string Reason) : PricingResult;
}

public static class InstitutionalPricing
{
    public const int MinimumQuantity = 20;
    public const decimal InstitutionalRate = 0.10m;

    public static PricingResult Price(IReadOnlyList<OrderLine> lines)
    {
        int totalQuantity = lines.Sum(line => line.Quantity);
        if (totalQuantity < MinimumQuantity)
        {
            return new PricingResult.Rejected(
                $"An institutional order needs at least {MinimumQuantity} items; this order has {totalQuantity}.");
        }

        List<PricedLine> priced = [.. lines.Select(PriceLine)];
        return new PricingResult.Priced(priced, priced.Sum(line => line.Total));
    }

    private static PricedLine PriceLine(OrderLine line)
    {
        decimal rate = Math.Max(InstitutionalRate, line.CampaignRate);
        decimal total = Math.Round(line.ListPrice * line.Quantity * (1 - rate), 2, MidpointRounding.AwayFromZero);
        return new PricedLine(line.Isbn, line.Quantity, rate, total);
    }
}
