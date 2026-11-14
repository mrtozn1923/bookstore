using Bookstore.Institutional.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Billing;

public sealed class InvoiceCalculator(IApprovedOrderQuery orders)
{
    // Kuruma kesilecek aylık fatura: o ay teslim edilecek onaylı siparişlerin toplamı
    public Task<decimal> MonthlyInvoiceAsync(int institutionId, int year, int month, CancellationToken ct = default) =>
        orders.GetApprovedTotalAsync(institutionId, year, month, ct);
}

public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services) =>
        services.AddScoped<InvoiceCalculator>();
}
