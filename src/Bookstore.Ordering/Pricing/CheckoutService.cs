using Bookstore.Ordering.Carts;

namespace Bookstore.Ordering.Pricing;

// Imperative shell: reads the clock, calls the core, writes the log
public sealed class CheckoutService(TimeProvider clock, Campaign campaign, TextWriter log)
{
    public CheckoutTotals Checkout(Cart cart, bool isMember)
    {
        DateTimeOffset now = clock.GetUtcNow();
        CheckoutTotals totals = CheckoutCalculator.Calculate(cart, isMember, campaign, now);
        log.WriteLine($"[checkout] {now:yyyy-MM-dd HH:mm}Z total={totals.Total}");
        return totals;
    }
}
