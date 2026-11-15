using Bookstore.Ordering.Functional;

namespace Bookstore.Ordering.Pricing;

public static class PriceRules
{
    public static Func<decimal, decimal> PercentOff(decimal rate) => amount => amount * (1 - rate);

    public static Func<decimal, decimal> AmountOff(decimal discount) => amount => Math.Max(0m, amount - discount);

    public static Func<decimal, decimal> AddVat(decimal rate) => amount => amount * (1 + rate);

    public static readonly Func<decimal, decimal> RoundToKurus =
        amount => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static readonly Func<decimal, decimal> Identity = amount => amount;

    public static Func<decimal, decimal> Combine(IEnumerable<Func<decimal, decimal>> rules) =>
        rules.Aggregate(Identity, (combined, rule) => combined.Then(rule));
}
