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
                // Outer: the whole call, retries included, never takes longer than 10 seconds.
                pipeline.AddTimeout(TimeSpan.FromSeconds(10));
                // Retry only transient failures (5xx, 408, 429, network errors, attempt timeouts), backing off with jitter.
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });
                // Inner: one attempt may take 4 seconds, above the provider's slowest normal answer (3 s, seen in the traces).
                pipeline.AddTimeout(TimeSpan.FromSeconds(4));
            });
        return services;
    }
}
