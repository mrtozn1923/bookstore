using Bookstore.Ordering.Data;
using Bookstore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.Checkout;

// The saga changes the Order aggregate through this interface, so its steps can be unit tested without a database.
public interface IOrderRepository
{
    Task<Order> GetAsync(Guid orderId, CancellationToken ct);
}

public sealed class EfOrderRepository(OrderingDbContext db) : IOrderRepository
{
    // Tracked: the changes are saved by Wolverine together with the saga state and the outgoing messages.
    public Task<Order> GetAsync(Guid orderId, CancellationToken ct) =>
        db.Orders.SingleAsync(o => o.Id == new OrderId(orderId), ct);
}
