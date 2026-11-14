using Bookstore.Catalog;
using Bookstore.Data;
using System.Diagnostics;
using Bookstore.Inventory;
using Bookstore.Notifications;
using Bookstore.Observability;
using Bookstore.Payments;
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
    public sealed record PaymentFailed(int OrderId) : PlaceOrderOutcome;
}

public sealed class OrderService(
    BookstoreContext db, ICatalogService catalog, IInventoryService inventory, IPaymentGateway payments,
    OrderConfirmationQueue confirmations, TimeProvider clock, OrderingMetrics metrics, ILogger<OrderService> logger)
{
    // Institutional orders (schools, libraries, companies) have many lines; retail carts have a few.
    private const int InstitutionalLineCount = 10;

    public async Task<PlaceOrderOutcome> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        long started = clock.GetTimestamp();
        metrics.CheckoutStarted();
        PlaceOrderOutcome outcome;
        int lineCount = 0;
        try
        {
            (outcome, lineCount) = await PlaceAsync(request, ct);
        }
        catch
        {
            metrics.CheckoutFinished("error", "unknown", clock.GetElapsedTime(started));
            throw;
        }
        metrics.CheckoutFinished(OutcomeName(outcome), lineCount >= InstitutionalLineCount ? "institutional" : "retail",
            clock.GetElapsedTime(started));
        if (outcome is PlaceOrderOutcome.Placed placed)
        {
            logger.LogInformation("Order {OrderId} placed: {ItemCount} items, {Total} {Currency}, took {ElapsedMs} ms",
                placed.Order.Id, placed.Order.Items.Count, placed.Order.Total, placed.Order.Currency,
                (int)clock.GetElapsedTime(started).TotalMilliseconds);
        }
        else
        {
            logger.LogWarning("Order not placed: {Outcome}, took {ElapsedMs} ms",
                outcome.GetType().Name, (int)clock.GetElapsedTime(started).TotalMilliseconds);
        }
        return outcome;
    }

    private static string OutcomeName(PlaceOrderOutcome outcome) => outcome switch
    {
        PlaceOrderOutcome.Placed => "placed",
        PlaceOrderOutcome.OutOfStock => "out_of_stock",
        PlaceOrderOutcome.PaymentFailed => "payment_failed",
        PlaceOrderOutcome.CartEmpty => "cart_empty",
        PlaceOrderOutcome.CartNotFound => "cart_not_found",
        _ => "cart_changed"
    };

    private async Task<(PlaceOrderOutcome Outcome, int LineCount)> PlaceAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == request.CartId, ct);
        if (cart is null)
        {
            return (new PlaceOrderOutcome.CartNotFound(), 0);
        }
        if (cart.Items.Count == 0)
        {
            return (new PlaceOrderOutcome.CartEmpty(), 0);
        }
        return (await PlaceFromCartAsync(cart, request, ct), cart.Items.Count);
    }

    private async Task<PlaceOrderOutcome> PlaceFromCartAsync(Cart cart, PlaceOrderRequest request, CancellationToken ct)
    {
        IReadOnlyDictionary<int, BookInfo> books =
            await catalog.GetByIdsAsync([.. cart.Items.Select(i => i.BookId)], ct);

        // Friendly early answer before any row is locked: which books are short?
        List<string> shortIsbns = [];
        using (Activity? check = BookstoreActivitySources.Ordering.StartActivity("check availability"))
        {
            check?.SetTag("bookstore.cart.line_count", cart.Items.Count);
            // One query for the whole cart: the per-line query was an N+1 (12 SELECTs for an institutional order).
            IReadOnlyDictionary<int, int> available =
                await inventory.GetAvailableAsync([.. cart.Items.Select(i => i.BookId)], ct);
            foreach (CartItem item in cart.Items)
            {
                if (available.GetValueOrDefault(item.BookId) < item.Quantity)
                {
                    shortIsbns.Add(books[item.BookId].Isbn);
                }
            }
        }
        if (shortIsbns.Count > 0)
        {
            return new PlaceOrderOutcome.OutOfStock(shortIsbns);
        }

        // Stock reservation, the order and the cart removal commit or roll back together.
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);

        ReservationResult reservation;
        using (Activity? reserve = BookstoreActivitySources.Ordering.StartActivity("reserve stock"))
        {
            reservation = await inventory.ReserveAsync([.. cart.Items.Select(i => new StockLine(i.BookId, i.Quantity))], ct);
            reserve?.SetTag("bookstore.stock.reserved", reservation.Succeeded);
        }
        if (!reservation.Succeeded)
        {
            return new PlaceOrderOutcome.OutOfStock([.. reservation.UnavailableBookIds.Select(id => books[id].Isbn)]);
        }

        Customer customer = await FindOrCreateCustomerAsync(request.CustomerName, request.CustomerEmail, ct);
        Order order = new()
        {
            Customer = customer,
            PlacedAtUtc = clock.GetUtcNow().UtcDateTime,
            Status = OrderStatus.PaymentPending
        };
        foreach (CartItem item in cart.Items)
        {
            BookInfo book = books[item.BookId];
            order.Items.Add(new OrderItem
            {
                BookId = book.Id,
                Title = book.Title,
                UnitPrice = book.Price,
                Quantity = item.Quantity,
                LineTotal = OrderItem.CalculateLineTotal(book.Price, item.Quantity)
            });
        }
        order.Total = order.Items.Sum(i => i.LineTotal);

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

        return await AuthorizePaymentAsync(order, ct);
    }

    private async Task<PlaceOrderOutcome> AuthorizePaymentAsync(Order order, CancellationToken ct)
    {
        // Parent of every HTTP attempt (retries included) that the resilience handler makes.
        using Activity? activity = BookstoreActivitySources.Ordering.StartActivity("authorize payment");
        activity?.SetTag("bookstore.order.id", order.Id);
        try
        {
            await payments.AuthorizeAsync(new PaymentRequest(order.Id, order.Total, order.Currency), ct);
            order.Status = OrderStatus.Paid;
            await db.SaveChangesAsync(ct);
            await confirmations.PublishAsync(order.Id, order.Customer.Email, ct);
            return new PlaceOrderOutcome.Placed(order);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or Polly.Timeout.TimeoutRejectedException)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, "Payment could not be authorized");
            logger.LogError(ex, "Payment for order {OrderId} failed", order.Id);
            order.Status = OrderStatus.Cancelled;
            await inventory.ReleaseAsync([.. order.Items.Select(i => new StockLine(i.BookId, i.Quantity))], ct);
            await db.SaveChangesAsync(ct);
            return new PlaceOrderOutcome.PaymentFailed(order.Id);
        }
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
