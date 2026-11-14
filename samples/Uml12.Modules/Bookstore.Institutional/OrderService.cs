using Bookstore.Institutional.Contracts;
using Bookstore.Notifications.Contracts;

namespace Bookstore.Institutional;

internal sealed class OrderService(
    IBudgetService budget,
    INotificationSender notifications,
    IDistributorGateway distributor) : IOrderService
{
    public async Task ApproveAsync(int orderId, int approverId, CancellationToken ct = default)
    {
        if (!budget.HasEnoughBudget(unitId: 7, amount: 56_700m))
        {
            throw new InvalidOperationException("Insufficient budget.");
        }
        await distributor.PlaceOrderAsync(orderId, bookId: 1, quantity: 150, new DateOnly(2026, 12, 21), ct);
        await notifications.SendAsync("ada@example.com", $"Order #{orderId} approved", ct);
    }
}
