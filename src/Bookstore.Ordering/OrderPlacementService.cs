using Bookstore.Inventory;

namespace Bookstore.Ordering;

public sealed class OrderPlacementService(IStockLedger stock)
{
    public async Task PlaceAsync(Order order, CancellationToken ct)
    {
        foreach (OrderLine line in order.Lines)
        {
            await stock.ReserveAsync(line.Isbn, line.Quantity, ct);
        }

        order.MarkPlaced();
    }
}
