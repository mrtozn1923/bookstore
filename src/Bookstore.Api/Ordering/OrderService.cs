using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Ordering;

public abstract record PlaceOrderOutcome
{
    public sealed record Placed(Order Order) : PlaceOrderOutcome;
    public sealed record CartNotFound : PlaceOrderOutcome;
    public sealed record CartEmpty : PlaceOrderOutcome;
    public sealed record OutOfStock(IReadOnlyList<string> Isbns) : PlaceOrderOutcome;
    public sealed record CartChanged : PlaceOrderOutcome;
}

public sealed class OrderService(
    BookstoreContext db,
    ICatalogService catalog,
    IInventoryService inventory,
    CouponService coupons,
    TimeProvider clock,
    ILogger<OrderService> logger)
{
    public async Task<PlaceOrderOutcome> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == request.CartId, ct);
        if (cart is null)
        {
            return new PlaceOrderOutcome.CartNotFound();
        }
        if (cart.Items.Count == 0)
        {
            return new PlaceOrderOutcome.CartEmpty();
        }

        IReadOnlyDictionary<int, BookInfo> books =
            await catalog.GetByIdsAsync([.. cart.Items.Select(i => i.BookId)], ct);

        // Stock reservation, the order and the cart removal commit or roll back together.
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);

        ReservationResult reservation = await inventory.ReserveAsync(
            [.. cart.Items.Select(i => new StockLine(i.BookId, i.Quantity))], ct);
        if (!reservation.Succeeded)
        {
            return new PlaceOrderOutcome.OutOfStock([.. reservation.UnavailableBookIds.Select(id => books[id].Isbn)]);
        }

        Customer customer = await FindOrCreateCustomerAsync(request.CustomerName, request.CustomerEmail, ct);
        Order order = new()
        {
            Customer = customer,
            PlacedAtUtc = clock.GetUtcNow().UtcDateTime,
            Status = OrderStatus.Placed
        };
        CartPrice price = CartPricing.Price(cart.Items, books);
        foreach (PricedLine line in price.Lines)
        {
            order.Items.Add(new OrderItem
            {
                BookId = line.Book.Id,
                Title = line.Book.Title,
                UnitPrice = line.Book.Price,
                Quantity = line.Quantity,
                LineTotal = line.LineTotal
            });
        }
        Coupon? coupon = cart.CouponCode is null ? null : await coupons.FindValidAsync(cart.CouponCode, price.Subtotal, ct);
        order.Subtotal = price.Subtotal;
        order.Discount = coupon is null ? 0m : CouponService.CalculateDiscount(coupon, price);
        order.Total = order.Subtotal - order.Discount;
        order.CouponCode = coupon?.Code;

        db.Orders.Add(order);
        db.Carts.Remove(cart);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The same cart was ordered by a parallel request in the meantime.
            return new PlaceOrderOutcome.CartChanged();
        }
        await transaction.CommitAsync(ct);

        logger.LogInformation("Order {OrderId} placed by customer {CustomerId} with coupon {CouponCode}, discount {Discount}",
            order.Id, customer.Id, order.CouponCode, order.Discount);

        return new PlaceOrderOutcome.Placed(order);
    }

    private async Task<Customer> FindOrCreateCustomerAsync(string name, string email, CancellationToken ct)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();
        Customer? customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == normalizedEmail, ct);
        if (customer is null)
        {
            customer = new Customer { Name = name.Trim(), Email = normalizedEmail };
            db.Customers.Add(customer);
        }
        return customer;
    }
}
