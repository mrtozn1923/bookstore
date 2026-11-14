using Bookstore.Institutional.Application;
using Bookstore.Institutional.Domain;

namespace Bookstore.Api.Demo;

// Yalnız yerel deneme için: --demo ile başlatılınca dış sistemlerin yerine geçer
public static class DemoServices
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        services.AddSingleton<ICatalogPrices, DemoCatalogPrices>();
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
