namespace Bookstore.Api.Services;

public static class PriceCalculator
{
    public static decimal ApplyDiscount(decimal price, int discountPercent)
    {
        if (discountPercent < 0 || discountPercent > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(discountPercent));
        }

        return Math.Round(price * (100 - discountPercent) / 100, 2);
    }
}
