using Bookstore.Contracts.Ordering;
using Bookstore.Inventory.V1;
using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Domain;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Bookstore.Ordering.Orders;

public abstract record PlaceOrderOutcome
{
    public sealed record Placed(Order Order) : PlaceOrderOutcome;
    public sealed record UnknownBooks(IReadOnlyList<string> Isbns) : PlaceOrderOutcome;
    public sealed record PricesChanged(IReadOnlyList<PriceChange> Changes) : PlaceOrderOutcome;
    public sealed record OutOfStock(IReadOnlyList<string> Isbns) : PlaceOrderOutcome;
    public sealed record InventoryUnavailable(StatusCode Status) : PlaceOrderOutcome;
}

// The write side: load what the aggregate needs, let it decide, reserve stock synchronously, save.
public sealed class PlaceOrderHandler(
    IDbContextOutbox<OrderingDbContext> outbox, InventoryService.InventoryServiceClient inventory, IConfiguration configuration,
    TimeProvider clock, ILogger<PlaceOrderHandler> logger)
{
    private readonly OrderingDbContext db = outbox.DbContext;

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

        // Sync call with its own deadline: Inventory gets at most this long, then the caller gives up.
        TimeSpan deadline = configuration.GetValue("Ordering:ReservationDeadline", TimeSpan.FromSeconds(2));
        try
        {
            ReserveStockResponse reserved = await inventory.ReserveStockAsync(
                new ReserveStockRequest
                {
                    OrderId = order.Id.ToString(),
                    Lines = { order.Items.Select(i => new ReservationLine { BookId = i.Isbn, Quantity = i.Quantity }) }
                },
                deadline: clock.GetUtcNow().UtcDateTime + deadline,
                cancellationToken: ct);
            order.MarkStockReserved(Guid.Parse(reserved.ReservationId), reserved.ExpiresAt.ToDateTime());
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            string unavailable = ex.Trailers.GetValue("unavailable-books") ?? "";
            return new PlaceOrderOutcome.OutOfStock(unavailable.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Aborted)
        {
            // Unknown result: the reservation may exist on Inventory's side. Its TTL releases it.
            logger.LogWarning("Stock reservation for order {OrderId} failed with {Status}", order.Id, ex.StatusCode);
            return new PlaceOrderOutcome.InventoryUnavailable(ex.StatusCode);
        }

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

        // INSERT Orders/OrderItems and the outbox row in one transaction, then hand the message to RabbitMQ.
        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new PlaceOrderOutcome.Placed(order);
    }

    private static Contracts.Ordering.OrderPlaced ToIntegrationEvent(Order order) => new(
        order.Id.Value,
        order.Buyer.Email,
        [.. order.Items.Select(i => new OrderPlacedLine(i.Isbn, i.Quantity, i.UnitPrice.Amount))],
        order.Total.Amount,
        order.Total.Currency,
        new DateTimeOffset(order.PlacedAtUtc, TimeSpan.Zero));
}
