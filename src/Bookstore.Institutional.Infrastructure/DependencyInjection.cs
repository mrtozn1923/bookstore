using Bookstore.Institutional.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Institutional.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInstitutionalOrdering(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IDiscountAgreements, DiscountAgreements>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddSingleton<IPriceCalculator, PriceCalculator>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();
        services.AddSingleton(TimeProvider.System);
        // Onaydan sonraki işler queue üzerinden: yayınlayan beklemez, consumer sırayla işler
        services.AddSingleton<ChannelEventBus>();
        services.AddSingleton<IIntegrationEventPublisher>(sp => sp.GetRequiredService<ChannelEventBus>());
        services.AddHostedService<OrderApprovedConsumer>();
        services.AddHttpClient<ICatalogPrices, CatalogPrices>(c => c.BaseAddress = new Uri("https://catalog.bookstore.test"));
        services.AddHttpClient<IDistributorGateway, DistributorGateway>(c => c.BaseAddress = new Uri("https://distributor.example.com"));
        return services;
    }
}
