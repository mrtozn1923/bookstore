using Bookstore.Ordering.Checkout;
using Bookstore.Ordering.Legacy;

namespace Bookstore.Fp.Demo;

public static class Post01
{
    public static void Guess()
    {
        List<LegacyCartLine> cart = SampleCarts.Legacy();
        Console.WriteLine($"lines before: {cart.Count}");

        decimal total = new LegacyCheckout().CalculateTotal(cart, "AUTUMN10", isMember: true);

        Console.WriteLine($"total: {total}");
        Console.WriteLine($"lines after: {cart.Count}");
        Console.WriteLine($"LastTotal: {LegacyCheckout.LastTotal}");
    }

    public static void Declarative()
    {
        List<LegacyCartLine> cart = SampleCarts.Legacy();

        decimal subtotal = CheckoutMath.Subtotal(cart);
        bool hasPrinted = CheckoutMath.HasPrintedBooks(cart);
        Console.WriteLine($"subtotal: {subtotal}, printed: {hasPrinted}, lines: {cart.Count}");

        Func<decimal, decimal> autumnCoupon = amount => amount * 0.90m;
        Func<decimal, decimal> memberDiscount = amount => amount - 15m;

        decimal discounted = memberDiscount(autumnCoupon(subtotal));
        decimal shipping = CheckoutMath.ShippingFee(discounted, hasPrinted);
        decimal total = Math.Round(discounted + shipping, 2, MidpointRounding.AwayFromZero);
        Console.WriteLine($"discounted: {discounted:0.00}, shipping: {shipping}, total: {total}");
    }
}
