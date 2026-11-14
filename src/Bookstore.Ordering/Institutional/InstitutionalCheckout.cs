using Bookstore.Inventory;
using Microsoft.Extensions.Logging;

namespace Bookstore.Ordering.Institutional;

// Checkout for bulk orders from schools, libraries and companies (at least 20 copies)
public sealed class InstitutionalCheckout(
    IOrderRepository orders,
    IStockService stock,
    IPriceRules prices,
    ICheckoutSession session,
    TimeProvider clock,
    ILogger<InstitutionalCheckout> logger)
{
    public const int MinimumQuantity = 20;

    public async Task<CheckoutReceipt> PlaceAsync(
        CustomerAccount customer, IReadOnlyList<OrderLine> lines, CancellationToken ct)
    {
        int quantity = lines.Sum(l => l.Quantity);
        if (customer.IsInstitutional && quantity < MinimumQuantity)
        {
            throw new InvalidOperationException(
                $"Institutional orders need at least {MinimumQuantity} copies; got {quantity}.");
        }

        foreach (OrderLine line in lines)
        {
            if (!stock.TryReserve(line.Isbn, line.Quantity, out int available))
            {
                throw new InsufficientStockException(line.Isbn, line.Quantity, available);
            }
        }

        decimal rate = prices.DiscountRate(customer, quantity);
        DateTimeOffset now = clock.GetUtcNow();
        string number = OrderNumber.Format(now.Year, orders.NextSequence(now.Year));
        Order order = new(
            Guid.NewGuid(),
            number,
            customer.Email,
            [.. lines.Select(l => l with { UnitPrice = Math.Round(l.UnitPrice * (1 - rate), 2, MidpointRounding.AwayFromZero) })]);

        await orders.SaveAsync(order, ct);
        if (orders is IUnitOfWork unitOfWork)
        {
            await unitOfWork.CommitAsync(ct);
        }

        session.LastOrderNumber = number;
        logger.LogInformation("Placed institutional order {OrderNumber} with {Quantity} copies", number, quantity);
        return new CheckoutReceipt(number, order.Total, now);
    }
}
