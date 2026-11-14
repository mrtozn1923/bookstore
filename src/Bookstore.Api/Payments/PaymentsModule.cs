using System.Net;
using Bookstore.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

namespace Bookstore.Payments;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddOptions<PaymentsOptions>()
            .BindConfiguration(PaymentsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<LastKnownGood<PaymentsOptions>>();

        // The per-call timeout is applied in PaymentGatewayClient (it can change at runtime); HttpClient.Timeout
        // is only an upper bound for the case where the switch is set too high.
        IHttpClientBuilder client = services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>((sp, http) =>
        {
            http.BaseAddress = sp.GetRequiredService<IOptions<PaymentsOptions>>().Value.BaseAddress;
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        // Fault injection for game days. Not even registered in Production; elsewhere it is off until
        // Chaos:Enabled is set (in switches.json, so a game day starts and aborts without a restart).
        bool chaosAllowed = !environment.IsProduction();
        if (chaosAllowed)
        {
            services.AddOptions<ChaosOptions>()
                .BindConfiguration(ChaosOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<LastKnownGood<ChaosOptions>>();
        }

        client.AddResilienceHandler("payments", (pipeline, context) =>
        {
            // Game day 2026-11-14: one retry after a 503 or a broken connection (the Idempotency-Key header makes the
            // second attempt safe). Timeouts are not retried: the per-call timeout already covers both attempts.
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => response.StatusCode == HttpStatusCode.ServiceUnavailable)
            });

            if (!chaosAllowed)
            {
                return;
            }
            // Chaos goes last (innermost), so the retry above sees injected faults like real ones.
            LastKnownGood<ChaosOptions> chaos = context.ServiceProvider.GetRequiredService<LastKnownGood<ChaosOptions>>();
            pipeline
                .AddChaosLatency(new ChaosLatencyStrategyOptions
                {
                    EnabledGenerator = _ => ValueTask.FromResult(chaos.Value.Enabled),
                    InjectionRateGenerator = _ => ValueTask.FromResult(chaos.Value.LatencyRate),
                    LatencyGenerator = _ => ValueTask.FromResult(TimeSpan.FromMilliseconds(chaos.Value.LatencyMs))
                })
                .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
                {
                    EnabledGenerator = _ => ValueTask.FromResult(chaos.Value.Enabled),
                    InjectionRateGenerator = _ => ValueTask.FromResult(chaos.Value.FaultRate),
                    OutcomeGenerator = _ => ValueTask.FromResult<Outcome<HttpResponseMessage>?>(
                        Outcome.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
                });
        });
        return services;
    }
}
