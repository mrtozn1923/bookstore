using System.Diagnostics;
using System.Security.Claims;
using Bookstore.Data;
using Bookstore.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional;

public sealed record PurchaseRequestBody(IReadOnlyList<RequestLine> Lines);

public sealed record PurchaseRequestLineView(int BookId, int Quantity, decimal UnitPrice);

public sealed record InstitutionOrderView(int Id, string Status, DateTime PlacedAtUtc, decimal Total);

public sealed record PurchaseRequestView(
    int Id, string Status, decimal Total, string RequestedBy, string? ApprovedBy, int? OrderId,
    IReadOnlyList<PurchaseRequestLineView> Lines);

public static class InstitutionalModule
{
    public const string Policy = "Institutional";

    public static IServiceCollection AddInstitutionalModule(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder().AddPolicy(Policy, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("institution_id")
            .RequireClaim("unit_id")
            .RequireRole(InstitutionalRoles.Buyer, InstitutionalRoles.Approver));
        return services.AddScoped<PurchaseRequestService>().AddScoped<AccessAudit>();
    }

    public static IEndpointRouteBuilder MapInstitutionalEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder requests = app.MapGroup("/api/institutions/me/purchase-requests")
            .WithTags("Institutional")
            .RequireAuthorization(Policy);
        requests.MapPost("/", Create);
        requests.MapGet("/{id:int}", Get).WithName("GetPurchaseRequest");
        requests.MapPut("/{id:int}/lines", ChangeLines);
        requests.MapPost("/{id:int}/approve", Approve);
        requests.MapPost("/{id:int}/order", PlaceOrder);

        // Institution portal (release 2): the orders of an institution.
        app.MapGet("/api/institutions/{institutionId:int}/orders", ListOrders)
            .WithTags("Institutional")
            .RequireAuthorization(Policy);
        return app;
    }

    private static async Task<IResult> Create(
        PurchaseRequestBody body, ClaimsPrincipal user, PurchaseRequestService service, CancellationToken ct) =>
        InstitutionalCaller.TryRead(user, out InstitutionalCaller? caller)
            ? ToResult(await service.CreateAsync(caller, body.Lines, ct), created: true)
            : TypedResults.Forbid();

    private static async Task<IResult> Get(int id, ClaimsPrincipal user, PurchaseRequestService service, CancellationToken ct)
    {
        if (!InstitutionalCaller.TryRead(user, out InstitutionalCaller? caller))
        {
            return TypedResults.Forbid();
        }
        PurchaseRequest? request = await service.FindAsync(caller, id, ct);
        return request is null ? TypedResults.NotFound() : TypedResults.Ok(ToView(request));
    }

    private static async Task<IResult> ChangeLines(
        int id, PurchaseRequestBody body, ClaimsPrincipal user, PurchaseRequestService service, CancellationToken ct) =>
        InstitutionalCaller.TryRead(user, out InstitutionalCaller? caller)
            ? ToResult(await service.ChangeLinesAsync(caller, id, body.Lines, ct))
            : TypedResults.Forbid();

    private static async Task<IResult> Approve(int id, ClaimsPrincipal user, PurchaseRequestService service, CancellationToken ct) =>
        InstitutionalCaller.TryRead(user, out InstitutionalCaller? caller)
            ? ToResult(await service.ApproveAsync(caller, id, ct))
            : TypedResults.Forbid();

    private static async Task<IResult> PlaceOrder(int id, ClaimsPrincipal user, PurchaseRequestService service, CancellationToken ct) =>
        InstitutionalCaller.TryRead(user, out InstitutionalCaller? caller)
            ? ToResult(await service.PlaceOrderAsync(caller, id, ct))
            : TypedResults.Forbid();

    private static async Task<IResult> ListOrders(
        int institutionId, ClaimsPrincipal user, BookstoreContext db, AccessAudit audit, CancellationToken ct)
    {
        if (!InstitutionalCaller.TryRead(user, out InstitutionalCaller? caller))
        {
            return TypedResults.Forbid();
        }
        // INC-2026-11-12: the institution in the route must be the caller's own; the token decides, not the URL.
        if (institutionId != caller.InstitutionId)
        {
            await audit.WriteAsync(caller, "institution.orders.list", institutionId, [], "denied", ct);
            return TypedResults.NotFound();
        }
        List<InstitutionOrderView> orders = await db.Orders.AsNoTracking()
            .Where(o => o.InstitutionId == institutionId)
            .OrderBy(o => o.Id)
            .Select(o => new InstitutionOrderView(o.Id, o.Status.ToString(), o.PlacedAtUtc, o.Total))
            .ToListAsync(ct);
        await audit.WriteAsync(caller, "institution.orders.list", institutionId, orders.Select(o => o.Id), "allowed", ct);
        return TypedResults.Ok(orders);
    }

    private static IResult ToResult(PurchaseOutcome outcome, bool created = false) => outcome switch
    {
        PurchaseOutcome.Done done when created =>
            TypedResults.CreatedAtRoute(ToView(done.Request), "GetPurchaseRequest", new { id = done.Request.Id }),
        PurchaseOutcome.Done done => TypedResults.Ok(ToView(done.Request)),
        PurchaseOutcome.NotFound => TypedResults.NotFound(),
        PurchaseOutcome.Forbidden forbidden => TypedResults.Problem(forbidden.Reason, statusCode: StatusCodes.Status403Forbidden),
        PurchaseOutcome.Invalid invalid => TypedResults.Problem(invalid.Reason, statusCode: StatusCodes.Status400BadRequest),
        PurchaseOutcome.Conflict conflict => TypedResults.Problem(conflict.Reason, statusCode: StatusCodes.Status409Conflict),
        _ => throw new UnreachableException()
    };

    private static PurchaseRequestView ToView(PurchaseRequest r) =>
        new(r.Id, r.Status.ToString(), r.Total, r.RequestedBy, r.ApprovedBy, r.OrderId,
            [.. r.Lines.Select(l => new PurchaseRequestLineView(l.BookId, l.Quantity, l.UnitPrice))]);
}
