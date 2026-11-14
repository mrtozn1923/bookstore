using Bookstore.Institutional.Domain;

namespace Bookstore.Institutional.Application;

public interface IOrderRepository
{
    Task<Order?> FindAsync(int id, CancellationToken ct);
    Task<InstitutionUser?> FindUserAsync(int id, CancellationToken ct);
    Task<BudgetCategory?> FindBudgetCategoryAsync(string code, CancellationToken ct);
    Task<bool> HasActiveOrderAsync(int userId, int bookId, CancellationToken ct);
    void Add(Order order);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IBudgetService
{
    Task<bool> HasEnoughBudgetAsync(Order order, CancellationToken ct);
    Task DeductAsync(Order order, CancellationToken ct);
    Task RefundAsync(Order order, CancellationToken ct);
}

public interface IPriceCalculator
{
    Money CalculateTotal(Money listPrice, int quantity, DiscountAgreement agreement);
}

public interface ICatalogPrices
{
    Task<Money?> GetListPriceAsync(int bookId, CancellationToken ct);
}

public interface IDiscountAgreements
{
    Task<DiscountAgreement> GetActiveAsync(int institutionId, DateOnly day, CancellationToken ct);
}

public interface IDistributorGateway
{
    Task PlaceOrderAsync(Order order, CancellationToken ct);
    Task CancelOrderAsync(int orderId, CancellationToken ct);
}

public interface INotificationSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
