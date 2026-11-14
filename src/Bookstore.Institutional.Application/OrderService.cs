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
    IIntegrationEventPublisher events,
    TimeProvider clock) : IOrderService
{
    private const int InstitutionId = 1; // Sample School

    public async Task<OrderResult> CreateAsync(CreateOrderCommand command, CancellationToken ct)
    {
        InstitutionUser user = await orders.FindUserAsync(command.PlacedById, ct)
            ?? throw new OrderRuleException($"User {command.PlacedById} not found.");
        BudgetCategory category = await orders.FindBudgetCategoryAsync(command.BudgetCategoryCode, ct)
            ?? throw new OrderRuleException($"Budget category {command.BudgetCategoryCode} not found.");

        Order order = new(user, category, command.BookId, command.Quantity, command.DeliveryDate, command.Note,
            clock.GetUtcNow());

        if (command.Submit)
            await SubmitAsync(order, ct);

        orders.Add(order);
        await orders.SaveChangesAsync(ct);
        return ToResult(order);
    }

    public async Task<OrderResult> SubmitAsync(int orderId, CancellationToken ct)
    {
        Order order = await LoadAsync(orderId, ct);
        await SubmitAsync(order, ct);
        await orders.SaveChangesAsync(ct);
        return ToResult(order);
    }

    private async Task SubmitAsync(Order order, CancellationToken ct)
    {
        Money total = await CalculateTotalAsync(order, ct);
        int unitId = order.PlacedBy.InstitutionUnitId;

        if (order.BudgetCategory.DeductsFromBudget &&
            !await budgets.HasEnoughBudgetAsync(unitId, order.BudgetCategory, order.DeliveryDate.Year, total.Amount, ct))
            throw new OrderRuleException($"Insufficient budget for an order total of {total.Amount} {total.Currency}.");

        if (await orders.HasActiveOrderAsync(order.PlacedById, order.BookId, ct))
            throw new OrderRuleException("An active order for this book already exists.");

        order.Submit(total, clock.GetUtcNow());
    }

    private async Task<Money> CalculateTotalAsync(Order order, CancellationToken ct)
    {
        Money listPrice = await catalog.GetListPriceAsync(order.BookId, ct)
            ?? throw new OrderRuleException($"Book {order.BookId} has no list price.");
        DiscountAgreement agreement = await agreements.GetActiveAsync(InstitutionId,
            DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), ct);
        return pricing.CalculateTotal(listPrice, order.Quantity, agreement);
    }

    public async Task<OrderResult> ApproveAsync(DecideOrderCommand command, CancellationToken ct)
    {
        Order order = await orders.FindAsync(command.OrderId, ct)
            ?? throw new OrderRuleException($"Order {command.OrderId} not found.");
        InstitutionUser approver = await orders.FindUserAsync(command.ApproverId, ct)
            ?? throw new OrderRuleException($"User {command.ApproverId} not found.");

        BudgetCategory category = order.BudgetCategory;
        int unitId = order.PlacedBy.InstitutionUnitId;
        int year = order.DeliveryDate.Year;

        // R8: onay anında bütçe yeniden kontrol edilir
        if (category.DeductsFromBudget &&
            !await budgets.HasEnoughBudgetAsync(unitId, category, year, order.Total.Amount, ct))
            throw new OrderRuleException("Budget is no longer sufficient.");

        ApprovalStep step = order.Status == OrderStatus.PendingPurchasingManager
            ? ApprovalStep.PurchasingManager
            : ApprovalStep.Approver;
        order.Approve(approver, command.Comment, clock.GetUtcNow());

        if (order.Status == OrderStatus.Approved && category.DeductsFromBudget)
            await budgets.DeductAsync(unitId, category, year, order.Total.Amount, ct);

        await orders.SaveChangesAsync(ct);
        await events.PublishAsync(new OrderApproved(order.Id, step, order.Status), ct);
        return ToResult(order);
    }

    public async Task<OrderResult> RejectAsync(DecideOrderCommand command, CancellationToken ct)
    {
        Order order = await LoadAsync(command.OrderId, ct);
        InstitutionUser approver = await orders.FindUserAsync(command.ApproverId, ct)
            ?? throw new OrderRuleException($"User {command.ApproverId} not found.");
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
            if (order.BudgetCategory.DeductsFromBudget)
                await budgets.RefundAsync(order.PlacedBy.InstitutionUnitId, order.BudgetCategory,
                    order.DeliveryDate.Year, order.Total.Amount, ct);
            await distributor.CancelOrderAsync(order.Id, ct);
        }
        await orders.SaveChangesAsync(ct);
        return ToResult(order);
    }

    private async Task<Order> LoadAsync(int id, CancellationToken ct) =>
        await orders.FindAsync(id, ct) ?? throw new OrderRuleException($"Order {id} not found.");

    private static OrderResult ToResult(Order order) => new(order.Id, order.Status.ToString(), order.Total.Amount);
}
