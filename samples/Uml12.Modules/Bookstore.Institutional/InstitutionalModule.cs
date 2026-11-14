using Bookstore.Institutional.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Institutional;

public static class InstitutionalModule
{
    // Port'lar: dışarıya yalnız iki provided interface açılıyor, implementasyonlar internal
    public static IServiceCollection AddInstitutionalModule(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IApprovedOrderQuery, ApprovedOrderQuery>();
        services.AddScoped<IBudgetService, BudgetService>();
        return services;
    }
}
