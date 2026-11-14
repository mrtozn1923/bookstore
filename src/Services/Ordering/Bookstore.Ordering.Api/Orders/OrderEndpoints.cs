using System.Diagnostics;
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
            .HasApiVersion(1.0);

        orders.MapPost("/", PlaceOrder).ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
        orders.MapGet("/{orderId:guid}", GetOrder).WithName("GetOrder").ProducesProblem(StatusCodes.Status404NotFound);
        orders.MapGet("/", ListOrders);
        return app;
    }

    // 202, not 201: the order exists, but stock and payment are decided later by the checkout saga.
    // The client follows the Location header to see the status change.
    private static async Task<Results<AcceptedAtRoute<OrderResponse>, ProblemHttpResult>> PlaceOrder(
        PlaceOrderRequest request, PlaceOrderHandler handler, OrderQueries queries, CancellationToken ct)
    {
        PlaceOrderOutcome outcome;
        try
        {
            outcome = await handler.HandleAsync(request, ct);
        }
        catch (DomainException ex)
        {
            return TypedResults.Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }

        return outcome switch
        {
            PlaceOrderOutcome.Placed placed => TypedResults.AcceptedAtRoute(
                (await queries.GetAsync(placed.Order.Id.Value, ct))!, "GetOrder", new { orderId = placed.Order.Id.Value, version = "1" }),
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
        Guid orderId, OrderQueries queries, CancellationToken ct) =>
        await queries.GetAsync(orderId, ct) is { } order
            ? TypedResults.Ok(order)
            : TypedResults.Problem(title: "Order not found", statusCode: StatusCodes.Status404NotFound);

    // Until post 10 the customer is a query parameter; then it comes from the token.
    private static async Task<Ok<IReadOnlyList<OrderSummaryResponse>>> ListOrders(
        string customerEmail, OrderQueries queries, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        TypedResults.Ok(await queries.ListByCustomerAsync(customerEmail, Math.Max(page, 1), Math.Clamp(pageSize, 1, 50), ct));
}
