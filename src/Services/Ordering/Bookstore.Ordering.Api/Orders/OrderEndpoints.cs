using System.Diagnostics;
using System.Security.Claims;
using Asp.Versioning;
using Bookstore.Ordering.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Ordering.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.NewVersionedApi("Ordering")
            .MapGroup("/api/v{version:apiVersion}/orders")
            .HasApiVersion(1.0)
            .RequireAuthorization("customer");

        orders.MapPost("/", PlaceOrder).ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
        orders.MapGet("/{orderId:guid}", GetOrder).WithName("GetOrder").ProducesProblem(StatusCodes.Status404NotFound);
        orders.MapGet("/", ListOrders);
        return app;
    }

    // 202, not 201: the order exists, but stock and payment are decided later by the checkout saga.
    // The client follows the Location header to see the status change.
    private static async Task<Results<AcceptedAtRoute<OrderResponse>, ProblemHttpResult>> PlaceOrder(
        PlaceOrderRequest request, ClaimsPrincipal user, PlaceOrderHandler handler, OrderQueries queries, CancellationToken ct)
    {
        Buyer buyer = BuyerFrom(user);
        PlaceOrderOutcome outcome;
        try
        {
            outcome = await handler.HandleAsync(request, buyer, ct);
        }
        catch (DomainException ex)
        {
            return TypedResults.Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }

        return outcome switch
        {
            PlaceOrderOutcome.Placed placed => TypedResults.AcceptedAtRoute(
                (await queries.GetAsync(placed.Order.Id.Value, buyer.Id, ct))!, "GetOrder", new { orderId = placed.Order.Id.Value, version = "1" }),
            PlaceOrderOutcome.UnknownBooks unknown => TypedResults.Problem(
                title: "Unknown books", detail: $"Not in the catalog: {string.Join(", ", unknown.Isbns)}.",
                statusCode: StatusCodes.Status400BadRequest),
            PlaceOrderOutcome.PricesChanged changed => TypedResults.Problem(
                title: "Prices changed", detail: "Some prices changed since the cart was shown. Confirm the new prices.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["changes"] = changed.Changes }),
            _ => throw new UnreachableException()
        };
    }

    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetOrder(
        Guid orderId, ClaimsPrincipal user, OrderQueries queries, CancellationToken ct) =>
        await queries.GetAsync(orderId, BuyerFrom(user).Id, ct) is { } order
            ? TypedResults.Ok(order)
            : TypedResults.Problem(title: "Order not found", statusCode: StatusCodes.Status404NotFound);

    // "My orders": the customer comes from the token, so nobody can list someone else's orders.
    private static async Task<Ok<IReadOnlyList<OrderSummaryResponse>>> ListOrders(
        ClaimsPrincipal user, OrderQueries queries, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        TypedResults.Ok(await queries.ListByCustomerAsync(BuyerFrom(user).Id, Math.Max(page, 1), Math.Clamp(pageSize, 1, 50), ct));

    // Claims as Keycloak issues them (MapInboundClaims = false keeps their original names).
    private static Buyer BuyerFrom(ClaimsPrincipal user) => new(
        user.FindFirstValue("sub") ?? throw new InvalidOperationException("The access token has no subject."),
        user.FindFirstValue("name") ?? "",
        (user.FindFirstValue("email") ?? "").ToLowerInvariant());
}
