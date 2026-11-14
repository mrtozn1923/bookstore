using System.Security.Claims;
using Bookstore.Api.Catalog;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Ordering;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder orders = routes.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization()
            .AddEndpointFilter<OrderLoggingFilter>()
            .AddEndpointFilterFactory(IdempotencyFilter.Create);

        orders.MapGet("/", List).WithName("ListOrders");
        orders.MapGet("/{id:int}", Get).WithName("GetOrder");
        orders.MapPost("/", Create).WithName("CreateOrder");
        orders.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelOrder")
            .WithSummary("Cancels an order that has not shipped yet.")
            .ProducesProblem(StatusCodes.Status409Conflict);
        orders.MapGet("/{id:int}/receipt", Receipt).WithName("GetOrderReceipt");

        return orders;
    }

    /// <summary>Lists the orders of the current customer.</summary>
    public static Ok<OrderDto[]> List(ClaimsPrincipal user, OrderStore store) =>
        TypedResults.Ok(store.ForCustomer(user.Identity!.Name!).ToArray());

    /// <summary>Gets one order of the current customer.</summary>
    /// <param name="id">The order number.</param>
    public static Results<Ok<OrderDto>, NotFound> Get(int id, ClaimsPrincipal user, OrderStore store) =>
        store.Find(id) is { } order && order.Customer == user.Identity!.Name
            ? TypedResults.Ok(order)
            : TypedResults.NotFound();

    /// <summary>Places a new order.</summary>
    /// <remarks>Send an Idempotency-Key header; a repeated key returns the first order instead of creating a new one.</remarks>
    [Idempotent]
    public static Results<Created<OrderDto>, ValidationProblem> Create(
        CreateOrderRequest request, ClaimsPrincipal user, BookCatalog catalog, OrderStore store)
    {
        List<OrderLine> lines = [];
        Dictionary<string, string[]> errors = [];
        foreach (OrderLineRequest item in request.Items)
        {
            if (Isbn.TryParse(item.Isbn, null, out Isbn isbn) && catalog.Find(isbn) is { } book)
            {
                lines.Add(new OrderLine(book.Isbn, book.Title, item.Quantity, book.Price));
            }
            else
            {
                errors[$"Items[{request.Items.IndexOf(item)}].Isbn"] = [$"No book with ISBN '{item.Isbn}'."];
            }
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        OrderDto order = store.Add(user.Identity!.Name!, lines);
        return TypedResults.Created($"/api/orders/{order.Id}", order);
    }

    public static Results<NoContent, NotFound, ProblemHttpResult> Cancel(int id, ClaimsPrincipal user, OrderStore store)
    {
        if (store.Find(id) is not { } order || order.Customer != user.Identity!.Name)
        {
            return TypedResults.NotFound();
        }
        if (order.Status == OrderStatus.Shipped)
        {
            return TypedResults.Problem(
                title: "Shipped orders cannot be cancelled.",
                statusCode: StatusCodes.Status409Conflict);
        }

        store.Update(order with { Status = OrderStatus.Cancelled });
        return TypedResults.NoContent();
    }

    /// <summary>Returns a plain-text receipt.</summary>
    public static Results<ReceiptResult, NotFound> Receipt(int id, ClaimsPrincipal user, OrderStore store) =>
        store.Find(id) is { } order && order.Customer == user.Identity!.Name
            ? new ReceiptResult(order)
            : TypedResults.NotFound();
}
