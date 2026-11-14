using Bookstore.Institutional.Application;
using Bookstore.Institutional.Domain;

namespace Bookstore.Api.Demo;

// Yalnız yerel deneme için: --demo ile başlatılınca dış sistemlerin yerine geçer
public static class DemoServices
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        services.AddSingleton<ICatalogPrices, DemoCatalogPrices>();
        services.AddSingleton<IDistributorGateway, LoggingDistributorGateway>();
        services.AddSingleton<INotificationSender, FlakyMailSender>();
        return services;
    }
}

// Catalog module'ünün liste fiyatları (Dune, Emma, The Hobbit)
internal sealed class DemoCatalogPrices : ICatalogPrices
{
    private static readonly Dictionary<int, decimal> Prices = new() { [1] = 420m, [2] = 380m, [3] = 450m };

    public Task<Money?> GetListPriceAsync(int bookId, CancellationToken ct) =>
        Task.FromResult<Money?>(Prices.TryGetValue(bookId, out decimal price) ? Money.Try(price) : null);
}

// Kitap dağıtıcısının API'si yerine yalnız log yazar
internal sealed class LoggingDistributorGateway(ILogger<LoggingDistributorGateway> logger) : IDistributorGateway
{
    public Task PlaceOrderAsync(Order order, CancellationToken ct)
    {
        logger.LogInformation("Distributor: place order {Id} (book {BookId} x{Quantity}, delivery {DeliveryDate:yyyy-MM-dd})",
            order.Id, order.BookId, order.Quantity, order.DeliveryDate);
        return Task.CompletedTask;
    }

    public Task CancelOrderAsync(int orderId, CancellationToken ct)
    {
        logger.LogInformation("Distributor: cancel order {Id}", orderId);
        return Task.CompletedTask;
    }
}

// E-posta sunucusu ilk denemede cevap vermiyor, sonrakilerde hemen gönderiyor
internal sealed class FlakyMailSender(ILogger<FlakyMailSender> logger) : INotificationSender
{
    private int calls;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        if (Interlocked.Increment(ref calls) == 1)
            await Task.Delay(Timeout.Infinite, ct);
        logger.LogInformation("Mail to {To}: {Subject}", to, subject);
    }
}
