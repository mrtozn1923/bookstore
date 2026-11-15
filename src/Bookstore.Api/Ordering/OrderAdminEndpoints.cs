using Bookstore.Catalog;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

public sealed record OrderRow(
    int Id, DateTime PlacedAtUtc, string CustomerName, string Status, int ItemCount, decimal Total, string Currency);

// Back office list of orders: filtered, sorted and paged in the database, never in the browser.
public static class OrderAdminEndpoints
{
    public static IEndpointRouteBuilder MapOrderAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/orders", SearchOrders).WithTags("Ordering");
        return app;
    }

    // GET /api/admin/orders?status=Shipped&sort=total&direction=desc&page=2&pageSize=25
    private static async Task<Results<Ok<PagedResult<OrderRow>>, ValidationProblem>> SearchOrders(
        BookstoreContext db, string? status, string? sort, string? direction, int page = 1, int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Order> query = db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, ignoreCase: true, out OrderStatus parsed))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = [$"Unknown status '{status}'."]
                });
            }
            query = query.Where(o => o.Status == parsed);
        }

        bool descending = !string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        // Every sort ends with the id, so a page boundary never splits rows with equal values differently.
        query = (sort?.ToLowerInvariant(), descending) switch
        {
            ("id", false) => query.OrderBy(o => o.Id),
            ("id", true) => query.OrderByDescending(o => o.Id),
            ("total", false) => query.OrderBy(o => o.Total).ThenBy(o => o.Id),
            ("total", true) => query.OrderByDescending(o => o.Total).ThenByDescending(o => o.Id),
            ("customer", false) => query.OrderBy(o => o.Customer.Name).ThenBy(o => o.Id),
            ("customer", true) => query.OrderByDescending(o => o.Customer.Name).ThenByDescending(o => o.Id),
            (_, false) => query.OrderBy(o => o.PlacedAtUtc).ThenBy(o => o.Id),
            (_, true) => query.OrderByDescending(o => o.PlacedAtUtc).ThenByDescending(o => o.Id)
        };

        int total = await query.CountAsync(ct);
        List<OrderRow> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderRow(
                o.Id, o.PlacedAtUtc, o.Customer.Name, o.Status.ToString(), o.Items.Count, o.Total, o.Currency))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<OrderRow>(items, total, page, pageSize));
    }
}
