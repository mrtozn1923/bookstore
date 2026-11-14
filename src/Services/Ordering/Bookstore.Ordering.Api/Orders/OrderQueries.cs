using Bookstore.Ordering.Data;
using Bookstore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.Orders;

// The read side: no aggregate, no tracking, only the columns the screen needs.
public sealed class OrderQueries(OrderingDbContext db)
{
    public async Task<OrderResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        OrderId orderId = new(id);
        return await db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderResponse(
                id, o.Status.ToString(), o.PlacedAtUtc, o.Buyer.Name, o.Buyer.Email,
                o.Items.Select(i => new OrderLineResponse(i.Isbn, i.Title, i.UnitPrice.Amount, i.Quantity, i.LineTotal.Amount)).ToList(),
                o.Total.Amount, o.Total.Currency, o.StockReservedUntilUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> ListByCustomerAsync(string email, int page, int pageSize, CancellationToken ct)
    {
        string normalized = email.Trim().ToLowerInvariant();
        var rows = await db.Orders.AsNoTracking()
            .Where(o => o.Buyer.Email == normalized)
            .OrderByDescending(o => o.PlacedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.PlacedAtUtc,
                ItemCount = o.Items.Sum(i => i.Quantity),
                o.Total.Amount,
                o.Total.Currency
            })
            .ToListAsync(ct);

        return [.. rows.Select(r => new OrderSummaryResponse(r.Id.Value, r.Status.ToString(), r.PlacedAtUtc, r.ItemCount, r.Amount, r.Currency))];
    }
}
