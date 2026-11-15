using Bookstore.Ordering.Legacy;

namespace Bookstore.Ordering.Checkout;

public static class CheckoutMath
{
    public static decimal Subtotal(IEnumerable<LegacyCartLine> lines) =>
        lines.Where(line => line.Quantity > 0)
             .Sum(line => line.UnitPrice * line.Quantity);

    public static bool HasPrintedBooks(IEnumerable<LegacyCartLine> lines) =>
        lines.Any(line => line.Quantity > 0 && !line.IsEbook);

    public static decimal ShippingFee(decimal discountedTotal, bool hasPrintedBooks) =>
        (hasPrintedBooks, discountedTotal) switch
        {
            (false, _) => 0m,
            (true, >= 300m) => 0m,
            _ => 49.90m
        };
}
