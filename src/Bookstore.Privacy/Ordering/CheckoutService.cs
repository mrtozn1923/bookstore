using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bookstore.Data;
using Bookstore.Privacy;
using Bookstore.Privacy.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookstore.Ordering;

public sealed record CheckoutRequest(
    [property: PersonalData(InformationType = "Contact Info")] string ShippingAddress,
    [property: PersonalData(InformationType = "Name")] string BillingName,
    decimal Total);

public sealed class CheckoutService(BookstoreContext db, TimeProvider clock, ILogger<CheckoutService> logger)
{
    public static readonly ActivitySource Source = new("Bookstore.Ordering");
    private static readonly Meter Meter = new("Bookstore.Ordering");
    private static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>("bookstore.orders.placed");

    public async Task<int> PlaceOrderAsync(int customerId, CheckoutRequest request, CancellationToken ct = default)
    {
        using Activity? activity = Source.StartActivity("checkout");
        Customer customer = await db.Customers.SingleAsync(c => c.Id == customerId, ct);

        DateTime now = clock.GetUtcNow().UtcDateTime;
        var order = new Order
        {
            CustomerId = customer.Id,
            OrderedAtUtc = now,
            RetainUntilUtc = RetentionPolicy.OrderBillingRetainUntil(now),
            Total = request.Total,
            ShippingAddress = request.ShippingAddress,
            BillingName = request.BillingName,
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // Identifiers only: the customer number points to the record, the record stays in the database.
        activity?.SetTag("order.id", order.Id);
        OrdersPlaced.Add(1, new KeyValuePair<string, object?>("channel", "web"));
        logger.LogInformation("Order {OrderId} placed by customer {CustomerId}", order.Id, customer.Id);
        return order.Id;
    }
}
