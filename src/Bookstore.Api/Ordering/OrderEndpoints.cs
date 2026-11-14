using System.Security.Claims;
using Bookstore.Api.Catalog;
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
        // POST creates; Idempotency-Key makes a retry after a lost response safe (ADR-0005).
        orders.MapPost("", CreateOrder).WithName("createOrder")
            .AddEndpointFilter((context, next) => context.HttpContext.RequestServices
                .GetRequiredService<IdempotencyStore>().InvokeAsync<CreateOrderRequest>(context, next));
        orders.MapGet("/{orderId:guid}", GetOrder).WithName("getOrder");
        // AIP-136 / Azure style custom method: POST /orders/{orderId}:cancel
        orders.MapPost("/{orderId:guid}:cancel", CancelOrder).WithName("cancelOrder");
        orders.MapPost("/{orderId:guid}/returns", CreateReturn).WithName("createReturn");
        orders.MapGet("/{orderId:guid}/returns/{returnId:guid}", GetReturn).WithName("getReturn");
        return app;
    }

    internal static readonly SortField<Order>[] SortFields =
        [new("createdAt", o => o.CreatedAt), new("total", o => o.Total)];
    internal static readonly SortField<Order> ById = new("id", o => o.Id.ToString());

    private static Results<Ok<Page<OrderResponse>>, ProblemHttpResult> ListMyOrders(
        HttpContext http, int? pageSize, string? pageToken, ClaimsPrincipal principal, OrderData data, Paging paging)
    {
        if (Paging.RejectUnknownParameters(http.Request, ["pageSize", "pageToken"]) is { } unknown)
        {
            return unknown;
        }

        DemoUser user = principal.Current();
        _ = Paging.ParseSort("-createdAt", SortFields, out List<SortSpec<Order>> order);
        return paging.Paginate(http, data.All.Where(o => o.CustomerId == user.Name), order, ById, user.Name,
            pageSize, pageToken, OrderResponse.From);
    }

    private static async Task<Results<Created<OrderResponse>, ProblemHttpResult>> CreateOrder(
        CreateOrderRequest request, ClaimsPrincipal principal, OrderData orders, CatalogData catalog,
        CancellationToken cancellationToken)
    {
        DemoUser user = principal.Current();
        if (user.Role != "Customer")
        {
            return ProblemCatalog.Create(ProblemCatalog.Forbidden);
        }

        List<OrderItem> items = [];
        foreach (CreateOrderItem line in request.Items)
        {
            if (catalog.Find(line.Isbn) is not { } book)
            {
                return ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/books/{line.Isbn}"]);
            }

            if (line.Quantity > book.StockQuantity)
            {
                return ProblemCatalog.Create(ProblemCatalog.InsufficientStock, [book.StockQuantity, book.Title],
                    new Dictionary<string, object?> { ["isbn"] = book.Isbn, ["availableQuantity"] = book.StockQuantity });
            }

            items.Add(new OrderItem(book.Isbn, book.Title, line.Quantity, book.Price));
        }

        // Stands in for the payment authorization, which takes a moment.
        await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);
        Order order = orders.Add(user.Name, items);
        return TypedResults.Created($"/orders/{order.Id}", OrderResponse.From(order));
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
            return ProblemCatalog.Create(ProblemCatalog.OrderNotCancellable, [orderId, order.Status]);
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
            return ProblemCatalog.Create(ProblemCatalog.OrderNotReturnable, [orderId, order.Status]);
        }

        OrderReturn created = data.AddReturn(order, request.Reason);
        return TypedResults.Created($"/orders/{orderId}/returns/{created.Id}", created);
    }

    private static Results<Ok<OrderReturn>, ProblemHttpResult> GetReturn(
        Guid orderId, Guid returnId, ClaimsPrincipal principal, OrderData data) =>
        Visible(orderId, principal, data) is not null && data.FindReturn(orderId, returnId) is { } found
            ? TypedResults.Ok(found)
            : ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/orders/{orderId}/returns/{returnId}"]);

    private static Order? Visible(Guid orderId, ClaimsPrincipal principal, OrderData data) =>
        data.Find(orderId) is { } order && OrderData.CanSee(principal.Current(), order) ? order : null;

    // Invisible and missing orders get the same answer, so ids cannot be probed.
    private static ProblemHttpResult OrderNotFound(Guid orderId) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/orders/{orderId}"]);
}
