namespace Bookstore.Ordering.Institutional;

public sealed record CustomerAccount(string Email, bool IsInstitutional, int CompletedOrders);

public sealed record CheckoutReceipt(string OrderNumber, decimal Total, DateTimeOffset PlacedAtUtc);

public interface IOrderRepository
{
    int NextSequence(int year);

    Task SaveAsync(Order order, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
}

public interface IStockService
{
    bool TryReserve(string isbn, int quantity, out int available);
}

public interface IPriceRules
{
    decimal DiscountRate(CustomerAccount customer, int totalQuantity);
}

public interface ICheckoutSession
{
    string? LastOrderNumber { get; set; }
}
