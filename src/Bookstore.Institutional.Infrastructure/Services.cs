using System.Net.Http.Json;
using Bookstore.Institutional.Application;
using Bookstore.Institutional.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Infrastructure;

internal sealed class OrderRepository(BookstoreContext db) : IOrderRepository
{
    private static readonly OrderStatus[] ActiveStatuses =
        [OrderStatus.PendingApprover, OrderStatus.PendingPurchasingManager, OrderStatus.Approved];

    public Task<Order?> FindAsync(int id, CancellationToken ct) =>
        db.Orders.Include(o => o.PlacedBy).Include(o => o.BudgetCategory).Include(o => o.Approvals).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<InstitutionUser?> FindUserAsync(int id, CancellationToken ct) =>
        db.InstitutionUsers.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<BudgetCategory?> FindBudgetCategoryAsync(string code, CancellationToken ct) =>
        db.BudgetCategories.FirstOrDefaultAsync(c => c.Code == code && c.IsActive, ct);

    public Task<bool> HasActiveOrderAsync(int userId, int bookId, CancellationToken ct) =>
        db.Orders.AnyAsync(o => o.PlacedById == userId && o.BookId == bookId && ActiveStatuses.Contains(o.Status), ct);

    public void Add(Order order) => db.Orders.Add(order);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

internal sealed class BudgetService(BookstoreContext db) : IBudgetService
{
    public async Task<bool> HasEnoughBudgetAsync(int unitId, BudgetCategory category, int year, decimal amount,
        CancellationToken ct)
    {
        UnitBudget? budget = await FindAsync(unitId, category, year, ct);
        return budget is not null && budget.RemainingAmount >= amount;
    }

    public async Task DeductAsync(int unitId, BudgetCategory category, int year, decimal amount, CancellationToken ct) =>
        (await FindAsync(unitId, category, year, ct))?.Deduct(amount);

    public async Task RefundAsync(int unitId, BudgetCategory category, int year, decimal amount, CancellationToken ct) =>
        (await FindAsync(unitId, category, year, ct))?.Refund(amount);

    private Task<UnitBudget?> FindAsync(int unitId, BudgetCategory category, int year, CancellationToken ct) =>
        db.UnitBudgets.FirstOrDefaultAsync(b => b.InstitutionUnitId == unitId
            && b.BudgetCategoryId == category.Id && b.Year == year, ct);
}

internal sealed class DiscountAgreements(BookstoreContext db) : IDiscountAgreements
{
    public async Task<DiscountAgreement> GetActiveAsync(int institutionId, DateOnly day, CancellationToken ct)
    {
        List<DiscountAgreement> agreements = await db.DiscountAgreements
            .Where(a => a.InstitutionId == institutionId).ToListAsync(ct);
        return agreements.FirstOrDefault(a => a.IsActiveOn(day)) ?? new DiscountAgreement(institutionId, 0.10m, day, null);
    }
}

internal sealed class CatalogPrices(HttpClient http) : ICatalogPrices
{
    public async Task<Money?> GetListPriceAsync(int bookId, CancellationToken ct) =>
        await http.GetFromJsonAsync<Money?>($"/api/books/{bookId}/list-price", ct);
}

internal sealed class DistributorGateway(HttpClient http) : IDistributorGateway
{
    public async Task PlaceOrderAsync(Order order, CancellationToken ct) =>
        (await http.PostAsJsonAsync("/orders", new { order.Id, order.BookId, order.Quantity, order.DeliveryDate }, ct))
            .EnsureSuccessStatusCode();

    public async Task CancelOrderAsync(int orderId, CancellationToken ct) =>
        (await http.DeleteAsync($"/orders/{orderId}", ct)).EnsureSuccessStatusCode();
}

internal sealed class LoggingNotificationSender : INotificationSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        Console.WriteLine($"mail to {to}: {subject} {body}");
        return Task.CompletedTask;
    }
}
