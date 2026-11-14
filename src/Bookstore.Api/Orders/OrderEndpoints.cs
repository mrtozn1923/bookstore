using Bookstore.Data;
using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Orders;

public record OrderLine(int BookId, int Quantity);
public record PlaceOrderRequest(Guid RequestId, int CustomerId, List<OrderLine> Lines);

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/orders", PlaceOrderAsync);
    }

    private static async Task<IResult> PlaceOrderAsync(PlaceOrderRequest request, BookstoreContext db, ILogger<PlaceOrderRequest> logger, CancellationToken ct)
    {
        // Same RequestId again (client retry): return the existing order.
        var existing = await db.Orders
            .TagWith("POST /orders: idempotency check")
            .Where(o => o.RequestId == request.RequestId)
            .Select(o => new { o.Id, o.OrderNumber, o.TotalAmount })
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            return Results.Ok(existing);
        }

        var bookIds = request.Lines.Select(l => l.BookId).ToList();
        var prices = await db.Books
            .TagWith("POST /orders: prices")
            .Where(b => bookIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Price, ct);

        var order = new Order
        {
            RequestId = request.RequestId,
            CustomerId = request.CustomerId,
            Items = request.Lines.Select(l => new OrderItem { BookId = l.BookId, Quantity = l.Quantity, UnitPrice = prices[l.BookId] }).ToList()
        };
        order.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        db.Orders.Add(order);

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(
            db,
            operation: (context, token) => context.SaveChangesAsync(acceptAllChangesOnSuccess: false, token),
            verifySucceeded: (context, token) => context.Orders.AsNoTracking().AnyAsync(o => o.RequestId == request.RequestId, token),
            ct);
        db.ChangeTracker.AcceptAllChanges();

        logger.LogInformation("Order {OrderNumber} placed for request {RequestId}", order.OrderNumber, order.RequestId);
        return Results.Created($"/orders/{order.Id}", new { order.Id, order.OrderNumber, order.TotalAmount });
    }
}
