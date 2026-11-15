namespace Bookstore.Shipping;

public static class FreeShippingPolicy
{
    public const decimal Threshold = 500.00m;

    public static bool Qualifies(decimal discountedSubtotal) => discountedSubtotal >= Threshold;
}
