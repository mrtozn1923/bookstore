using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.Orders;

public abstract record PlaceOrderOutcome
{
    public sealed record Placed(Order Order) : PlaceOrderOutcome;
    public sealed record UnknownBooks(IReadOnlyList<string> Isbns) : PlaceOrderOutcome;
    public sealed record PricesChanged(IReadOnlyList<PriceChange> Changes) : PlaceOrderOutcome;
}

// The write side: load what the aggregate needs, let it decide, save. No call to another service.
public sealed class PlaceOrderHandler(OrderingDbContext db, TimeProvider clock, ILogger<PlaceOrderHandler> logger)
{
    public async Task<PlaceOrderOutcome> HandleAsync(PlaceOrderRequest request, CancellationToken ct)
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
            new Buyer(request.CustomerName.Trim(), request.CustomerEmail.Trim().ToLowerInvariant()),
            [.. request.Items.Select(i => new OrderLine(
                i.Isbn, books[i.Isbn].Title, new Money(books[i.Isbn].Price, books[i.Isbn].Currency), i.Quantity))],
            clock);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // Post 06 turns OrderPlaced into an integration event through the outbox.
        foreach (object domainEvent in order.DomainEvents)
        {
            logger.LogInformation("Domain event raised: {EventType} {@Event}", domainEvent.GetType().Name, domainEvent);
        }
        order.ClearDomainEvents();

        return new PlaceOrderOutcome.Placed(order);
    }
}
