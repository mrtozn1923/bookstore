namespace Bookstore.Ordering;

public sealed record PriceLine(string Isbn, decimal UnitPrice, int Quantity);

public sealed record PriceQuote(decimal Subtotal, decimal Discount, decimal Total);

// Prices a cart: line totals rounded per line, then the institutional discount (Requirements Analysis series).
public static class CartPricing
{
    public const int InstitutionalMinimumQuantity = 20;
    public const decimal InstitutionalDiscountRate = 0.10m;

    public static PriceQuote Quote(ReadOnlySpan<PriceLine> lines, bool institutional)
    {
        decimal subtotal = 0m;
        int quantity = 0;
        foreach (PriceLine line in lines)
        {
            subtotal += OrderItem.CalculateLineTotal(line.UnitPrice, line.Quantity);
            quantity += line.Quantity;
        }

        decimal discount = institutional && quantity >= InstitutionalMinimumQuantity
            ? Math.Round(subtotal * InstitutionalDiscountRate, 2, MidpointRounding.AwayFromZero)
            : 0m;
        return new PriceQuote(subtotal, discount, subtotal - discount);
    }
}
