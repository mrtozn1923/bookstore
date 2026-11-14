namespace Bookstore.Institutional.Contracts;

// Provided interface 1: API'nin kullandığı işlem yüzü
public interface IOrderService
{
    Task ApproveAsync(int orderId, int approverId, CancellationToken ct = default);
}

// Provided interface 2: Faturalama modülünün okuduğu sorgu yüzü
public interface IApprovedOrderQuery
{
    Task<decimal> GetApprovedTotalAsync(int institutionId, int year, int month, CancellationToken ct = default);
}

// Required interface: sahibi Institutional, implementasyonunu başka bir component verir
public interface IDistributorGateway
{
    Task PlaceOrderAsync(int orderId, int bookId, int quantity, DateOnly deliveryDate, CancellationToken ct = default);
}
