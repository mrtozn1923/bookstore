using Bookstore.Institutional.Application;
using Bookstore.Institutional.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api;

public static class OrderEndpoints
{
    // Use case -> endpoint eşlemesi (UC-01, UC-02, UC-03, UC-04)
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.MapGroup("/orders");

        orders.MapPost("/", async (CreateOrderCommand body, IOrderService service, CancellationToken ct) =>
        {
            OrderResult result = await service.CreateAsync(body, ct);
            return TypedResults.Created($"/orders/{result.Id}", result);
        });
        orders.MapPost("/{id:int}/submit", (int id, IOrderService s, CancellationToken ct) => s.SubmitAsync(id, ct));
        orders.MapPost("/{id:int}/approve", (int id, DecisionBody body, IOrderService s, CancellationToken ct) =>
            s.ApproveAsync(new DecideOrderCommand(id, body.ApproverId, body.Comment), ct));
        orders.MapPost("/{id:int}/reject", (int id, DecisionBody body, IOrderService s, CancellationToken ct) =>
            s.RejectAsync(new DecideOrderCommand(id, body.ApproverId, body.Comment), ct));
        orders.MapPost("/{id:int}/cancel", (int id, IOrderService s, CancellationToken ct) => s.CancelAsync(id, ct));
        orders.MapGet("/{id:int}", (int id, IOrderService s, CancellationToken ct) => s.FindAsync(id, ct));

        app.MapGet("/institution-units/{id:int}/budgets", (int id, BookstoreContext db, CancellationToken ct) =>
            db.UnitBudgets
                .Where(b => b.InstitutionUnitId == id)
                .Join(db.BudgetCategories, b => b.BudgetCategoryId, c => c.Id,
                    (b, c) => new UnitBudgetResult(c.Code, b.Year, b.AllocatedAmount, b.SpentAmount,
                        b.AllocatedAmount - b.SpentAmount))
                .ToListAsync(ct));
    }
}

public sealed record DecisionBody(int ApproverId, string? Comment);
