using Bookstore.Institutional.Domain;

namespace Bookstore.Institutional.Application;

public interface IOrderService
{
    Task<OrderResult> CreateAsync(CreateOrderCommand command, CancellationToken ct);
    Task<OrderResult> SubmitAsync(int orderId, CancellationToken ct);
    Task<OrderResult> ApproveAsync(DecideOrderCommand command, CancellationToken ct);
    Task<OrderResult> RejectAsync(DecideOrderCommand command, CancellationToken ct);
    Task<OrderResult> CancelAsync(int orderId, CancellationToken ct);
}

public sealed class OrderService(
    IOrderRepository orders,
    IBudgetService budgets,
    IPriceCalculator pricing,
    ICatalogPrices catalog,
    IDiscountAgreements agreements,
    IDistributorGateway distributor,
    INotificationSender notifications,
    TimeProvider clock) : IOrderService
{
    private const int InstitutionId = 1; // Sample School

    public async Task<OrderResult> CreateAsync(CreateOrderCommand command, CancellationToken ct)
    {
        InstitutionUser user = await orders.FindUserAsync(command.PlacedById, ct)
            ?? throw new InvalidOperationException($"User {command.PlacedById} not found.");
        BudgetCategory category = await orders.FindBudgetCategoryAsync(command.BudgetCategoryCode, ct)
            ?? throw new InvalidOperationException($"Budget category {command.BudgetCategoryCode} not found.");
        Order order = new(user, category, command.BookId, command.Quantity, command.DeliveryDate, command.Note,
            clock.GetUtcNow());
        orders.Add(order);
        await orders.SaveChangesAsync(ct);
        return ToResult(order);
    }

    public async Task<OrderResult> SubmitAsync(int orderId, CancellationToken ct)
    {
        Order order = await LoadAsync(orderId, ct);
        Money listPrice = await catalog.GetListPriceAsync(order.BookId, ct)
            ?? throw new InvalidOperationException($"Book {order.BookId} has no list price.");
        DiscountAgreement agreement = await agreements.GetActiveAsync(InstitutionId,
            DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), ct);
        order.Submit(pricing.CalculateTotal(listPrice, order.Quantity, agreement), clock.GetUtcNow());
        if (await orders.HasActiveOrderAsync(order.PlacedById, order.BookId, ct))
            throw new InvalidOperationException("An active order for this book already exists.");
        if (!await budgets.HasEnoughBudgetAsync(order, ct))
            throw new InvalidOperationException("Insufficient budget");
        await orders.SaveChangesAsync(ct);
        return ToResult(order);
    }

    public async Task<OrderResult> ApproveAsync(DecideOrderCommand command, CancellationToken ct)
    {
        Order order = await LoadAsync(command.OrderId, ct);
        InstitutionUser approver = await orders.FindUserAsync(command.ApproverId, ct)
            ?? throw new InvalidOperationException($"User {command.ApproverId} not found.");
        if (!await budgets.HasEnoughBudgetAsync(order, ct))
            throw new InvalidOperationException("Insufficient budget");
        order.Approve(approver, command.Comment, clock.GetUtcNow());
        if (order.Status == OrderStatus.Approved)
        {
            await budgets.DeductAsync(order, ct);
            await distributor.PlaceOrderAsync(order, ct);
        }
        await orders.SaveChangesAsync(ct);
        await notifications.SendAsync(order.PlacedBy.Email, $"Order {order.Id}", order.Status.ToString(), ct);
        return ToResult(order);
    }

    public async Task<OrderResult> RejectAsync(DecideOrderCommand command, CancellationToken ct)
    {
        Order order = await LoadAsync(command.OrderId, ct);
        InstitutionUser approver = await orders.FindUserAsync(command.ApproverId, ct)
            ?? throw new InvalidOperationException($"User {command.ApproverId} not found.");
        order.Reject(approver, command.Comment ?? "", clock.GetUtcNow());
        await orders.SaveChangesAsync(ct);
        await notifications.SendAsync(order.PlacedBy.Email, $"Order {order.Id}", order.Status.ToString(), ct);
        return ToResult(order);
    }

    public async Task<OrderResult> CancelAsync(int orderId, CancellationToken ct)
    {
        Order order = await LoadAsync(orderId, ct);
        bool wasApproved = order.Status == OrderStatus.Approved;
        order.Cancel(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
        if (wasApproved)
        {
            await budgets.RefundAsync(order, ct);
            await distributor.CancelOrderAsync(order.Id, ct);
        }
        await orders.SaveChangesAsync(ct);
        return ToResult(order);
    }

    private async Task<Order> LoadAsync(int id, CancellationToken ct) =>
        await orders.FindAsync(id, ct) ?? throw new InvalidOperationException($"Order {id} not found.");

    private static OrderResult ToResult(Order order) => new(order.Id, order.Status.ToString(), order.Total.Amount);
}
