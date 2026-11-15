using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Institutional;

public enum ShipmentSlot { This, Next }

public sealed record InstitutionalOrderLine(int Id, string Isbn, string Title, int Quantity, ShipmentSlot Shipment);

public sealed record InstitutionalOrderView(int Id, string Unit, InstitutionalOrderLine[] Lines);

public sealed record ShipmentPlan(int[] ThisShipment, int[] NextShipment);

// Demo: one institutional order whose lines can be split into two shipments (partial shipments, Release 2).
public sealed class InstitutionalOrderStore
{
    private readonly Lock gate = new();
    private InstitutionalOrderLine[] lines =
    [
        new(1, "9780441172719", "Dune", 30, ShipmentSlot.This),
        new(2, "9780261102217", "The Hobbit", 25, ShipmentSlot.This),
        new(3, "9780141439518", "Pride and Prejudice", 20, ShipmentSlot.This),
        new(4, "9780141439549", "Middlemarch", 20, ShipmentSlot.Next),
        new(5, "9780451524935", "1984", 40, ShipmentSlot.This),
        new(6, "9780141439471", "Frankenstein", 20, ShipmentSlot.Next)
    ];

    public InstitutionalOrderView Get()
    {
        lock (gate)
        {
            return new InstitutionalOrderView(1042, "Main Campus Library", [.. lines]);
        }
    }

    public bool TryApply(ShipmentPlan plan)
    {
        lock (gate)
        {
            int[] all = [.. plan.ThisShipment, .. plan.NextShipment];
            if (plan.ThisShipment.Length == 0 || all.Length != lines.Length || all.Distinct().Count() != all.Length
                || !lines.All(l => all.Contains(l.Id)))
            {
                return false;
            }
            // The order of each list is the packing order
            lines = [.. plan.ThisShipment.Select(id => lines.Single(l => l.Id == id) with { Shipment = ShipmentSlot.This }),
                     .. plan.NextShipment.Select(id => lines.Single(l => l.Id == id) with { Shipment = ShipmentSlot.Next })];
            return true;
        }
    }
}

public static class InstitutionalOrderEndpoints
{
    public static IServiceCollection AddInstitutionalOrders(this IServiceCollection services) =>
        services.AddSingleton<InstitutionalOrderStore>();

    public static IEndpointRouteBuilder MapInstitutionalOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/institutional-orders").WithTags("Institutional");
        group.MapGet("/{id:int}", GetOrder);
        group.MapPut("/{id:int}/shipments", SavePlan);
        return app;
    }

    private static Results<Ok<InstitutionalOrderView>, NotFound> GetOrder(int id, InstitutionalOrderStore store) =>
        id == 1042 ? TypedResults.Ok(store.Get()) : TypedResults.NotFound();

    // Every line exactly once, and the first shipment must not be empty.
    // ?notifyUnit=true: the unit is told about the plan (only logged here until the Notifications module sends e-mails)
    private static Results<NoContent, NotFound, ValidationProblem> SavePlan(
        int id, ShipmentPlan plan, InstitutionalOrderStore store, ILoggerFactory loggers, bool notifyUnit = false)
    {
        if (id != 1042)
        {
            return TypedResults.NotFound();
        }
        if (!store.TryApply(plan))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["lines"] = ["Every line must be in exactly one shipment and the first shipment cannot be empty."]
            });
        }
        if (notifyUnit)
        {
            loggers.CreateLogger("Bookstore.Institutional").LogInformation(
                "Shipment plan of order {OrderId} saved, notifying {Unit}", id, store.Get().Unit);
        }
        return TypedResults.NoContent();
    }
}
