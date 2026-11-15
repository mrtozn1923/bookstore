using Bookstore.Data;
using Bookstore.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

public sealed record OrderStatus(int OrderId, DateTime OrderedAtUtc, decimal Total);

// Sent in the request body, never in the URL: URLs end up in access logs, proxies and browser history.
public sealed record OrderLookupRequest([property: PersonalData(InformationType = "Contact Info")] string Email, int OrderId);

public sealed class OrderLookupService(BookstoreContext db)
{
    // Guest order tracking: the customer types the order number and the e-mail address.
    // "Not found" is a normal result, not an exception whose message would carry the e-mail address.
    public Task<OrderStatus?> FindAsync(OrderLookupRequest request, CancellationToken ct = default) =>
        db.Orders
            .Where(o => o.Id == request.OrderId && o.Customer != null && o.Customer.Email == request.Email)
            .Select(o => new OrderStatus(o.Id, o.OrderedAtUtc, o.Total))
            .SingleOrDefaultAsync(ct);
}
