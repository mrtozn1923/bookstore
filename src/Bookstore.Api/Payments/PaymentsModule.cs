using Bookstore.Configuration;
using Microsoft.Extensions.Options;

namespace Bookstore.Payments;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddOptions<PaymentsOptions>()
            .BindConfiguration(PaymentsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<LastKnownGood<PaymentsOptions>>();

        // No retries on purpose: one attempt, so every provider failure shows up in the SLI.
        // The per-call timeout is applied in PaymentGatewayClient (it can change at runtime); HttpClient.Timeout
        // is only an upper bound for the case where the switch is set too high.
        services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>((sp, client) =>
        {
            client.BaseAddress = sp.GetRequiredService<IOptions<PaymentsOptions>>().Value.BaseAddress;
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
