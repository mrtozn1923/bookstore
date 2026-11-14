namespace Bookstore.Ordering.Api.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.MapGroup("/api/orders");

        orders.MapPost("/", PlaceOrderAsync);
        orders.MapGet("/{id:guid}", (Guid id, OrderStore store) =>
            store.Find(id) is Order order ? Results.Ok(order) : Results.NotFound());

        return app;
    }

    private static async Task<IResult> PlaceOrderAsync(
        PlaceOrderRequest request, CatalogClient catalog, OrderStore store, TimeProvider clock, CancellationToken ct)
    {
        if (request.Items.Count == 0)
        {
            return Results.Problem("An order needs at least one item.", statusCode: StatusCodes.Status400BadRequest);
        }

        List<OrderLine> lines = [];
        foreach (OrderItemRequest item in request.Items)
        {
            CatalogBook? book = await catalog.GetBookAsync(item.Isbn, ct);
            if (book is null)
            {
                return Results.Problem($"Unknown book: {item.Isbn}", statusCode: StatusCodes.Status400BadRequest);
            }

            lines.Add(new OrderLine(book.Isbn, book.Title, book.Price, item.Quantity));
        }

        Order order = new(Guid.CreateVersion7(), request.CustomerEmail, lines, lines.Sum(l => l.LineTotal), "TRY", clock.GetUtcNow());
        store.Add(order);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
}
