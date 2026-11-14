using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Bookstore.Payments;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        Uri baseAddress = new(configuration["Payments:BaseAddress"]
            ?? throw new InvalidOperationException("Payments:BaseAddress is missing."));

        services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>(client => client.BaseAddress = baseAddress)
            .AddResilienceHandler("payment-provider", pipeline =>
            {
                // Outer: up to 3 retries, 500 ms apart. Inner: every attempt gets 2 seconds.
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false
                });
                pipeline.AddTimeout(TimeSpan.FromSeconds(2));
            });
        return services;
    }
}
