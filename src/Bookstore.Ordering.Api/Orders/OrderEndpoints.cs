using System.Diagnostics;

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
        PlaceOrderRequest request,
        CatalogClient catalog,
        OrderStore store,
        OrderingTelemetry telemetry,
        TimeProvider clock,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (request.Items.Count == 0)
        {
            return Results.Problem("An order needs at least one item.", statusCode: StatusCodes.Status400BadRequest);
        }

        using Activity? activity = OrderingTelemetry.ActivitySource.StartActivity("Fetch catalog prices");
        activity?.SetTag("bookstore.order.item_count", request.Items.Count);

        IReadOnlyList<CatalogBook> books = await catalog.GetBooksAsync(request.Items.Select(i => i.Isbn), ct);
        Dictionary<string, CatalogBook> byIsbn = books.ToDictionary(b => b.Isbn);

        List<OrderLine> lines = [];
        foreach (OrderItemRequest item in request.Items)
        {
            if (!byIsbn.TryGetValue(item.Isbn, out CatalogBook? book))
            {
                return Results.Problem($"Unknown book: {item.Isbn}", statusCode: StatusCodes.Status400BadRequest);
            }

            lines.Add(new OrderLine(book.Isbn, book.Title, book.Price, item.Quantity));
        }

        activity?.Stop();

        Order order = new(Guid.CreateVersion7(), request.CustomerEmail, lines, lines.Sum(l => l.LineTotal), "TRY", clock.GetUtcNow());
        store.Add(order);
        telemetry.OrderPlaced(order);
        loggerFactory.CreateLogger("Bookstore.Ordering.Orders").LogInformation("Order {OrderId} placed with {ItemCount} items, total {Total} {Currency}",
            order.Id, order.Lines.Count, order.Total, order.Currency);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
}
