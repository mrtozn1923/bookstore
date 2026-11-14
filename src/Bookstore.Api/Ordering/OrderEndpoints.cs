using System.Security.Claims;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Ordering;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.MapGroup("/orders").RequireAuthorization();
        orders.MapGet("", ListMyOrders).WithName("listOrders");
        orders.MapGet("/{orderId:guid}", GetOrder).WithName("getOrder");
        // AIP-136 / Azure style custom method: POST /orders/{orderId}:cancel
        orders.MapPost("/{orderId:guid}:cancel", CancelOrder).WithName("cancelOrder");
        orders.MapPost("/{orderId:guid}/returns", CreateReturn).WithName("createReturn");
        orders.MapGet("/{orderId:guid}/returns/{returnId:guid}", GetReturn).WithName("getReturn");
        return app;
    }

    private static Results<Ok<Page<OrderResponse>>, ProblemHttpResult> ListMyOrders(
        int? pageSize, string? pageToken, ClaimsPrincipal principal, OrderData data)
    {
        DemoUser user = principal.Current();
        List<OrderResponse> items =
        [
            .. data.All.Where(o => o.CustomerId == user.Name).OrderByDescending(o => o.CreatedAt).Select(OrderResponse.From),
        ];
        return Paging.Paginate(items, pageSize, pageToken);
    }

    private static Results<Ok<OrderResponse>, ProblemHttpResult> GetOrder(
        Guid orderId, ClaimsPrincipal principal, OrderData data) =>
        Visible(orderId, principal, data) is { } order
            ? TypedResults.Ok(OrderResponse.From(order))
            : OrderNotFound(orderId);

    private static Results<Ok<OrderResponse>, ProblemHttpResult> CancelOrder(
        Guid orderId, ClaimsPrincipal principal, OrderData data)
    {
        if (Visible(orderId, principal, data) is not { } order)
        {
            return OrderNotFound(orderId);
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            return TypedResults.Problem(statusCode: 409, title: "Order cannot be cancelled",
                detail: $"Order {orderId} is {order.Status}; only Pending or Confirmed orders can be cancelled.");
        }

        order.Status = OrderStatus.Cancelled;
        return TypedResults.Ok(OrderResponse.From(order));
    }

    private static Results<Created<OrderReturn>, ProblemHttpResult> CreateReturn(
        Guid orderId, CreateReturnRequest request, ClaimsPrincipal principal, OrderData data)
    {
        if (Visible(orderId, principal, data) is not { } order)
        {
            return OrderNotFound(orderId);
        }

        if (order.Status != OrderStatus.Delivered)
        {
            return TypedResults.Problem(statusCode: 409, title: "Order cannot be returned",
                detail: $"Order {orderId} is {order.Status}; only Delivered orders can be returned.");
        }

        OrderReturn created = data.AddReturn(order, request.Reason);
        return TypedResults.Created($"/orders/{orderId}/returns/{created.Id}", created);
    }

    private static Results<Ok<OrderReturn>, ProblemHttpResult> GetReturn(
        Guid orderId, Guid returnId, ClaimsPrincipal principal, OrderData data) =>
        Visible(orderId, principal, data) is not null && data.FindReturn(orderId, returnId) is { } found
            ? TypedResults.Ok(found)
            : TypedResults.Problem(statusCode: 404, title: "Return not found",
                detail: $"No return {returnId} exists for order {orderId}.");

    private static Order? Visible(Guid orderId, ClaimsPrincipal principal, OrderData data) =>
        data.Find(orderId) is { } order && OrderData.CanSee(principal.Current(), order) ? order : null;

    // Invisible and missing orders get the same answer, so ids cannot be probed.
    private static ProblemHttpResult OrderNotFound(Guid orderId) =>
        TypedResults.Problem(statusCode: 404, title: "Order not found", detail: $"No order {orderId} exists.");
}
