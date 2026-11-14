using Bookstore.Institutional.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.DistributorIntegration;

// Kitap dağıtıcısına giden HTTP çağrısı burada saklı; Institutional yalnız interface'i görüyor
internal sealed class AcmeDistributorGateway : IDistributorGateway
{
    public Task PlaceOrderAsync(int orderId, int bookId, int quantity, DateOnly deliveryDate, CancellationToken ct = default)
    {
        Console.WriteLine($"  [Distributor] order #{orderId} sent to Acme Book Distribution: book {bookId} x {quantity}, delivery {deliveryDate:yyyy-MM-dd}");
        return Task.CompletedTask;
    }
}

public static class DistributorIntegrationModule
{
    public static IServiceCollection AddDistributorIntegration(this IServiceCollection services) =>
        services.AddSingleton<IDistributorGateway, AcmeDistributorGateway>();
}
