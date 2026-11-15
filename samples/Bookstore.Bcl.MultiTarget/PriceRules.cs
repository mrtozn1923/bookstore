namespace Bookstore.Bcl.MultiTarget;

public static class PriceRules
{
    public static decimal ApplyDiscount(decimal price, decimal rate)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rate, 1m);
#else
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price must be non-negative.");
        if (rate > 1m)
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "Rate must be at most 1.");
#endif
        return price * (1 - rate);
    }
}
