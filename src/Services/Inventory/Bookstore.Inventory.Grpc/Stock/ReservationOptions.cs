namespace Bookstore.Inventory.Stock;

public sealed class ReservationOptions
{
    // An order has this long to be confirmed (post 07); then the stock goes back on sale.
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);
}
