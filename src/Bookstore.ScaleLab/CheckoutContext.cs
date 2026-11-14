using Microsoft.EntityFrameworkCore;

namespace Bookstore.ScaleLab;

// a minimal checkout context: only what the failover demo needs
public sealed class CheckoutContext(DbContextOptions<CheckoutContext> options) : DbContext(options)
{
    public Task<int> PlaceOrderAsync(Guid idempotencyKey, int customerId, decimal total, CancellationToken ct) =>
        Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO orders (customer_id, status, total_amount, created_at, idempotency_key)
            VALUES ({customerId}, 'Pending', {total}, now(), {idempotencyKey})
            ON CONFLICT (idempotency_key) DO NOTHING
            """, ct);
}
