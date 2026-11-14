namespace Bookstore.Inventory.Stock;

public sealed class ReservationOptions
{
    // Longer than the checkout saga's payment timeout (15 minutes): the saga releases the stock itself;
    // the TTL is only the safety net for a saga that never finishes.
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(20);
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);
}
