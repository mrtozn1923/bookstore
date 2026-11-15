namespace Bookstore.Prompts.Tasks.CodePrompts;

// 1-19 copies: no discount; 20-5000 copies: 10%; larger orders need a quote
public static class InstitutionalDiscount
{
    public const int MinimumQuantity = 20;
    public const int MaximumQuantity = 5000;
    public const decimal Rate = 0.10m;

    public static decimal RateFor(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, MaximumQuantity);
        return quantity >= MinimumQuantity ? Rate : 0m;
    }
}
