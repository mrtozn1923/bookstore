using Bookstore.Contracts.Inventory;
using Bookstore.Contracts.Ordering;
using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Checkout;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Bookstore.Ordering.Orders;

public abstract record PlaceOrderOutcome
{
    public sealed record Placed(Order Order) : PlaceOrderOutcome;
    public sealed record UnknownBooks(IReadOnlyList<string> Isbns) : PlaceOrderOutcome;
    public sealed record PricesChanged(IReadOnlyList<PriceChange> Changes) : PlaceOrderOutcome;
}

// The write side: load what the aggregate needs, let it decide, save, and hand the rest to the checkout saga.
public sealed class PlaceOrderHandler(IDbContextOutbox<OrderingDbContext> outbox, TimeProvider clock)
{
    private readonly OrderingDbContext db = outbox.DbContext;

    public async Task<PlaceOrderOutcome> HandleAsync(PlaceOrderRequest request, Buyer buyer, CancellationToken ct)
    {
        string[] isbns = [.. request.Items.Select(i => i.Isbn).Distinct()];
        Dictionary<string, CatalogBook> books = await db.CatalogBooks.AsNoTracking()
            .Where(b => isbns.Contains(b.Isbn))
            .ToDictionaryAsync(b => b.Isbn, ct);

        string[] unknown = [.. isbns.Where(isbn => !books.ContainsKey(isbn))];
        if (unknown.Length > 0)
        {
            return new PlaceOrderOutcome.UnknownBooks(unknown);
        }

        // The price is frozen here: the order gets the price the customer confirmed, or nothing.
        PriceChange[] changes =
        [
            .. request.Items
                .Where(i => books[i.Isbn].Price != i.UnitPrice)
                .Select(i => new PriceChange(i.Isbn, i.UnitPrice, books[i.Isbn].Price))
        ];
        if (changes.Length > 0)
        {
            return new PlaceOrderOutcome.PricesChanged(changes);
        }

        Order order = Order.Place(
            buyer,
            [.. request.Items.Select(i => new OrderLine(
                i.Isbn, books[i.Isbn].Title, new Money(books[i.Isbn].Price, books[i.Isbn].Currency), i.Quantity))],
            clock);

        db.Orders.Add(order);

        // The domain event stays inside Ordering; what leaves the service is the integration event.
        foreach (object domainEvent in order.DomainEvents)
        {
            if (domainEvent is Domain.OrderPlaced)
            {
                await outbox.PublishAsync(ToIntegrationEvent(order));
            }
        }
        order.ClearDomainEvents();

        // No call to Inventory here: the saga reserves stock with a message. Checkout no longer stops when Inventory is down.
        await outbox.SendAsync(new StartCheckout(
            order.Id.Value,
            [.. order.Items.Select(i => new StockLine(i.Isbn, i.Quantity))],
            order.Total.Amount,
            order.Total.Currency,
            order.Buyer.Id,
            order.Buyer.Email));

        // INSERT Orders/OrderItems and both outbox rows in one transaction, then the messages leave.
        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new PlaceOrderOutcome.Placed(order);
    }

    private static Contracts.Ordering.OrderPlaced ToIntegrationEvent(Order order) => new(
        order.Id.Value,
        order.Buyer.Id,
        order.Buyer.Email,
        [.. order.Items.Select(i => new OrderPlacedLine(i.Isbn, i.Quantity, i.UnitPrice.Amount))],
        order.Total.Amount,
        order.Total.Currency,
        new DateTimeOffset(order.PlacedAtUtc, TimeSpan.Zero));
}
