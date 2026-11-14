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

        orders.MapPost("/", PlaceOrder).ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict).ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        orders.MapGet("/{orderId:guid}", GetOrder).WithName("GetOrder").ProducesProblem(StatusCodes.Status404NotFound);
        orders.MapGet("/", ListOrders);
        return app;
    }

    private static async Task<Results<CreatedAtRoute<OrderResponse>, ProblemHttpResult>> PlaceOrder(
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
            PlaceOrderOutcome.Placed placed => TypedResults.CreatedAtRoute(
                (await queries.GetAsync(placed.Order.Id.Value, ct))!, "GetOrder", new { orderId = placed.Order.Id.Value, version = "1" }),
            PlaceOrderOutcome.UnknownBooks unknown => TypedResults.Problem(
                title: "Unknown books", detail: $"Not in the catalog: {string.Join(", ", unknown.Isbns)}.",
                statusCode: StatusCodes.Status400BadRequest),
            PlaceOrderOutcome.PricesChanged changed => TypedResults.Problem(
                title: "Prices changed", detail: "Some prices changed since the cart was shown. Confirm the new prices.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["changes"] = changed.Changes }),
            PlaceOrderOutcome.OutOfStock outOfStock => TypedResults.Problem(
                title: "Out of stock", detail: $"Not enough stock for: {string.Join(", ", outOfStock.Isbns)}.",
                statusCode: StatusCodes.Status409Conflict),
            PlaceOrderOutcome.InventoryUnavailable unavailable => TypedResults.Problem(
                title: "Checkout is temporarily unavailable",
                detail: "Stock could not be reserved. No order was created; please try again in a moment.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["grpcStatus"] = unavailable.Status.ToString() }),
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
