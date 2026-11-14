using System.Diagnostics;
using Bookstore.Catalog;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

public static class OrderingModule
{
    public static IServiceCollection AddOrderingModule(this IServiceCollection services) =>
        services.AddScoped<OrderService>().AddScoped<CouponService>();

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder carts = app.MapGroup("/api/carts").WithTags("Ordering");
        carts.MapPost("/", CreateCart);
        carts.MapGet("/{cartId:guid}", GetCart).WithName("GetCart");
        carts.MapPut("/{cartId:guid}/items/{isbn}", SetItemQuantity);
        carts.MapDelete("/{cartId:guid}/items/{isbn}", RemoveItem);
        carts.MapPut("/{cartId:guid}/coupon", ApplyCoupon);
        carts.MapDelete("/{cartId:guid}/coupon", RemoveCoupon);

        RouteGroupBuilder orders = app.MapGroup("/api/orders").WithTags("Ordering");
        orders.MapPost("/", PlaceOrder);
        orders.MapGet("/{orderId:int}", GetOrder).WithName("GetOrder");
        return app;
    }

    private static async Task<CreatedAtRoute<CartView>> CreateCart(BookstoreContext db, TimeProvider clock, CancellationToken ct)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        Cart cart = new() { Id = Guid.CreateVersion7(), CreatedAtUtc = now, UpdatedAtUtc = now };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);
        return TypedResults.CreatedAtRoute(
            new CartView(cart.Id, [], 0m, 0m, 0m, "TRY", null, null), "GetCart", new { cartId = cart.Id });
    }

    private static async Task<Results<Ok<CartView>, NotFound>> GetCart(
        Guid cartId, BookstoreContext db, ICatalogService catalog, CouponService coupons, CancellationToken ct)
    {
        Cart? cart = await db.Carts.AsNoTracking().Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        return cart is null ? TypedResults.NotFound() : TypedResults.Ok(await ToViewAsync(cart, catalog, coupons, ct));
    }

    // PUT /api/carts/{cartId}/items/{isbn}  { "quantity": 2 }  → adds the book or changes its quantity
    private static async Task<Results<Ok<CartView>, NotFound<string>>> SetItemQuantity(
        Guid cartId, string isbn, SetQuantityRequest request,
        BookstoreContext db, ICatalogService catalog, CouponService coupons, TimeProvider clock, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
        {
            return TypedResults.NotFound("Cart not found.");
        }
        BookInfo? book = await catalog.FindByIsbnAsync(isbn, ct);
        if (book is null)
        {
            return TypedResults.NotFound($"Book {isbn} not found.");
        }

        CartItem? item = cart.Items.FirstOrDefault(i => i.BookId == book.Id);
        if (item is null)
        {
            cart.Items.Add(new CartItem { BookId = book.Id, Quantity = request.Quantity });
        }
        else
        {
            item.Quantity = request.Quantity;
        }
        cart.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(await ToViewAsync(cart, catalog, coupons, ct));
    }

    private static async Task<Results<Ok<CartView>, NotFound>> RemoveItem(
        Guid cartId, string isbn, BookstoreContext db, ICatalogService catalog, CouponService coupons, TimeProvider clock, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        BookInfo? book = await catalog.FindByIsbnAsync(isbn, ct);
        if (cart is null || book is null)
        {
            return TypedResults.NotFound();
        }

        cart.Items.RemoveAll(i => i.BookId == book.Id);
        cart.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await ToViewAsync(cart, catalog, coupons, ct));
    }

    private static async Task<Results<CreatedAtRoute<OrderView>, NotFound, ProblemHttpResult>> PlaceOrder(
        PlaceOrderRequest request, OrderService orders, CancellationToken ct)
    {
        PlaceOrderOutcome outcome = await orders.PlaceOrderAsync(request, ct);
        return outcome switch
        {
            PlaceOrderOutcome.Placed placed =>
                TypedResults.CreatedAtRoute(ToView(placed.Order), "GetOrder", new { orderId = placed.Order.Id }),
            PlaceOrderOutcome.CartNotFound => TypedResults.NotFound(),
            PlaceOrderOutcome.CartEmpty =>
                TypedResults.Problem("The cart is empty.", statusCode: StatusCodes.Status400BadRequest),
            PlaceOrderOutcome.OutOfStock outOfStock =>
                TypedResults.Problem(
                    $"Not enough stock for: {string.Join(", ", outOfStock.Isbns)}.",
                    statusCode: StatusCodes.Status409Conflict),
            PlaceOrderOutcome.CartChanged =>
                TypedResults.Problem("The cart changed while the order was being placed.",
                    statusCode: StatusCodes.Status409Conflict),
            _ => throw new UnreachableException()
        };
    }

    private static async Task<Results<Ok<OrderView>, NotFound>> GetOrder(int orderId, BookstoreContext db, CancellationToken ct)
    {
        Order? order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(ToView(order));
    }

    // PUT /api/carts/{cartId}/coupon  { "code": "BOOK10" }
    private static async Task<Results<Ok<CartView>, NotFound, ProblemHttpResult>> ApplyCoupon(
        Guid cartId, ApplyCouponRequest request, IConfiguration config,
        BookstoreContext db, ICatalogService catalog, CouponService coupons, TimeProvider clock, CancellationToken ct)
    {
        if (!config.GetValue<bool>("Features:Coupons"))
        {
            return TypedResults.NotFound();
        }
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
        {
            return TypedResults.NotFound();
        }

        IReadOnlyDictionary<int, BookInfo> books = await catalog.GetByIdsAsync([.. cart.Items.Select(i => i.BookId)], ct);
        CartPrice price = CartPricing.Price(cart.Items, books);
        Coupon? coupon = await coupons.FindValidAsync(request.Code, price.Subtotal, ct);
        if (coupon is null)
        {
            return TypedResults.Problem($"Coupon {request.Code} is not valid for this cart.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        cart.CouponCode = coupon.Code;
        cart.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await ToViewAsync(cart, catalog, coupons, ct));
    }

    private static async Task<Results<Ok<CartView>, NotFound>> RemoveCoupon(
        Guid cartId, BookstoreContext db, ICatalogService catalog, CouponService coupons, TimeProvider clock, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
        {
            return TypedResults.NotFound();
        }
        cart.CouponCode = null;
        cart.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await ToViewAsync(cart, catalog, coupons, ct));
    }

    private static async Task<CartView> ToViewAsync(Cart cart, ICatalogService catalog, CouponService coupons, CancellationToken ct)
    {
        IReadOnlyDictionary<int, BookInfo> books = await catalog.GetByIdsAsync([.. cart.Items.Select(i => i.BookId)], ct);
        CartPrice price = CartPricing.Price(cart.Items, books);

        Coupon? coupon = cart.CouponCode is null ? null : await coupons.FindValidAsync(cart.CouponCode, price.Subtotal, ct);
        decimal discount = coupon is null ? 0m : await coupons.CalculateDiscountAsync(coupon, price, ct);

        return new CartView(
            cart.Id,
            [.. price.Lines.Select(l => new CartLineView(l.Book.Isbn, l.Book.Title, l.Book.Price, l.Quantity, l.LineTotal))],
            price.Subtotal,
            discount,
            price.Subtotal - discount,
            "TRY",
            coupon?.Code,
            coupon?.Description);
    }

    private static OrderView ToView(Order order) =>
        new(order.Id, order.Status.ToString(), order.PlacedAtUtc,
            [.. order.Items.Select(i => new OrderLineView(i.Title, i.UnitPrice, i.Quantity, i.LineTotal))],
            order.Subtotal, order.Discount, order.Total, order.Currency, order.CouponCode);
}
