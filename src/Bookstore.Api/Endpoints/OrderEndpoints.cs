using System.Security.Claims;
using Bookstore.Api.Data;
using Bookstore.Api.Integrations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Bookstore.Api.Endpoints;

public sealed record OrderLineRequest(int BookId, int Quantity);

public sealed record PlaceOrderRequest(List<OrderLineRequest> Items, string ShippingCity, string? CouponCode = null);

public sealed record OrderDto(int Id, string Reference, OrderStatus Status, decimal Total, string? TrackingNumber, DateTimeOffset CreatedAt);

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        orders.MapPost("/", PlaceOrderAsync);
        orders.MapGet("/{id:int}", GetOrderAsync);
        orders.MapPost("/{id:int}/cancel", CancelOrderAsync);
        return app;
    }

    private static async Task<Results<Created<OrderDto>, ValidationProblem, ProblemHttpResult>> PlaceOrderAsync(
        PlaceOrderRequest request,
        ClaimsPrincipal user,
        BookstoreDbContext db,
        PaymentClient payments,
        ShippingClient shipping,
        IOrderEmailSender email,
        TimeProvider clock,
        ILogger<PlaceOrderRequest> logger,
        CancellationToken ct)
    {
        if (request.Items.Count == 0 || request.Items.Any(i => i.Quantity <= 0))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["An order needs at least one item with a positive quantity."]
            });
        }

        int customerId = int.Parse(user.FindFirstValue("customer_id")!);
        Customer customer = await db.Customers.SingleAsync(c => c.Id == customerId, ct);

        List<int> bookIds = request.Items.Select(i => i.BookId).ToList();
        Dictionary<int, Book> books = await db.Books.Where(b => bookIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, ct);

        foreach (OrderLineRequest line in request.Items)
        {
            if (!books.TryGetValue(line.BookId, out Book? book) || book.Stock < line.Quantity)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Insufficient stock",
                    detail: $"Book {line.BookId} does not have {line.Quantity} copies in stock.");
            }
        }

        int percentOff = 0;
        if (request.CouponCode is not null)
        {
            Coupon? coupon = await CouponEndpoints.FindValidAsync(db, request.CouponCode, clock.GetUtcNow(), ct);
            if (coupon is null)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["couponCode"] = [$"Coupon {request.CouponCode} is not valid."]
                });
            }
            percentOff = coupon.PercentOff;
        }

        Order order = new()
        {
            Reference = $"ORD-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            CustomerId = customerId,
            CreatedAt = clock.GetUtcNow(),
            Status = OrderStatus.Placed,
            Items = request.Items.Select(i => new OrderItem
            {
                BookId = i.BookId,
                Quantity = i.Quantity,
                UnitPrice = books[i.BookId].Price
            }).ToList()
        };
        decimal subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.Total = Math.Round(subtotal * (100 - percentOff) / 100m, 2, MidpointRounding.AwayFromZero);

        PaymentResult payment;
        try
        {
            payment = await payments.ChargeAsync(new ChargeRequest(order.Reference, order.Total, "TRY"), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Payment provider failed for {OrderReference}", order.Reference);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Payment provider unavailable",
                detail: "The order was not placed. Please try again later.");
        }

        if (!payment.Approved)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Payment declined",
                detail: payment.DeclineReason);
        }

        order.PaymentId = payment.ChargeId;
        foreach (OrderItem item in order.Items)
        {
            books[item.BookId].Stock -= item.Quantity;
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        try
        {
            order.TrackingNumber = await shipping.CreateShipmentAsync(
                new ShipmentRequest(order.Reference, request.ShippingCity, order.Items.Sum(i => i.Quantity)), ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Shipment could not be created for {OrderReference}; it will be retried later", order.Reference);
        }

        await email.SendOrderConfirmationAsync(customer.Email, order.Reference, order.Total, ct);

        return TypedResults.Created($"/api/orders/{order.Id}", ToDto(order));
    }

    private static async Task<Results<Ok<OrderDto>, NotFound>> GetOrderAsync(
        int id, ClaimsPrincipal user, BookstoreDbContext db, CancellationToken ct)
    {
        int customerId = int.Parse(user.FindFirstValue("customer_id")!);
        Order? order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(ToDto(order));
    }

    private static async Task<Results<Ok<OrderDto>, NotFound, ProblemHttpResult>> CancelOrderAsync(
        int id, ClaimsPrincipal user, BookstoreDbContext db, TimeProvider clock, CancellationToken ct)
    {
        int customerId = int.Parse(user.FindFirstValue("customer_id")!);
        Order? order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (!OrderRules.CanCancel(order.CreatedAt, clock.GetUtcNow()))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cancellation window closed",
                detail: $"Orders can only be cancelled within {OrderRules.CancellationWindow.TotalMinutes} minutes.");
        }

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToDto(order));
    }

    private static OrderDto ToDto(Order order) =>
        new(order.Id, order.Reference, order.Status, order.Total, order.TrackingNumber, order.CreatedAt);
}

public static class OrderRules
{
    public static readonly TimeSpan CancellationWindow = TimeSpan.FromMinutes(30);

    public static bool CanCancel(DateTimeOffset createdAt, DateTimeOffset now) => now - createdAt <= CancellationWindow;
}
