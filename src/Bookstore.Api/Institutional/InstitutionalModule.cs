using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Institutional;

public sealed record PurchaseRequestBody(IReadOnlyList<RequestLine> Lines);

public sealed record PurchaseRequestLineView(int BookId, int Quantity, decimal UnitPrice);

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
        return services.AddScoped<PurchaseRequestService>();
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
