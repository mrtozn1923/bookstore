using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace Bookstore.ResilienceLab.Demos;

// The same carrier client, registered the old way (Microsoft.Extensions.Http.Polly, Polly v7 API)
// and the new way (Microsoft.Extensions.Http.Resilience, Polly v8).
public static class MigrationDemos
{
    // Before: AddPolicyHandler with a v7 policy, as in many .NET Core 2.1-7 code bases.
    public static async Task PollyV7Async()
    {
        using var server = new Flaky([500, 500]);
        var services = new ServiceCollection();
        services.AddHttpClient("legacy-carrier", c => c.BaseAddress = server.Url)
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (outcome, delay, attempt, _) =>
                        Console.WriteLine($"v7 retry {attempt} after {(int?)outcome.Result?.StatusCode}, waiting {delay.TotalMilliseconds} ms")))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2)));
        await using var provider = services.BuildServiceProvider();

        using var response = await provider.GetRequiredService<IHttpClientFactory>().CreateClient("legacy-carrier").GetAsync("api/v2/quotes");
        Console.WriteLine($"v7 -> {(int)response.StatusCode} after {server.Attempts} attempts");
    }

    // After: one resilience handler, strategies added from the outside in.
    public static async Task PollyV8Async()
    {
        using var server = new Flaky([500, 500]);
        var services = new ServiceCollection();
        services.AddHttpClient("legacy-carrier", c => c.BaseAddress = server.Url)
            .AddResilienceHandler("carrier", pipeline => pipeline
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    BackoffType = DelayBackoffType.Linear,
                    Delay = TimeSpan.FromMilliseconds(200),
                    UseJitter = false,
                    OnRetry = args =>
                    {
                        Console.WriteLine($"v8 retry {args.AttemptNumber + 1} after {(int?)args.Outcome.Result?.StatusCode}, waiting {args.RetryDelay.TotalMilliseconds} ms");
                        return default;
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(2)));
        await using var provider = services.BuildServiceProvider();

        using var response = await provider.GetRequiredService<IHttpClientFactory>().CreateClient("legacy-carrier").GetAsync("api/v2/quotes");
        Console.WriteLine($"v8 -> {(int)response.StatusCode} after {server.Attempts} attempts");
    }

    // In between: a new v8 circuit breaker inside an old v7 policy chain, while the rest is migrated.
    public static async Task BridgeAsync()
    {
        using var server = new Flaky([500, 500, 500, 500, 500, 500]);
        var breaker = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build()
            .AsAsyncPolicy();
        var retry = HttpPolicyExtensions.HandleTransientHttpError()
            .Or<BrokenCircuitException>()
            .RetryAsync(5, onRetry: (outcome, attempt) =>
                Console.WriteLine($"v7 retry {attempt}: {(outcome.Exception is { } ex ? ex.GetType().Name : ((int)outcome.Result.StatusCode).ToString())}"));

        var services = new ServiceCollection();
        services.AddHttpClient("legacy-carrier", c => c.BaseAddress = server.Url)
            .AddPolicyHandler(Policy.WrapAsync(retry, breaker));
        await using var provider = services.BuildServiceProvider();

        try
        {
            await provider.GetRequiredService<IHttpClientFactory>().CreateClient("legacy-carrier").GetAsync("api/v2/quotes");
        }
        catch (BrokenCircuitException ex)
        {
            Console.WriteLine($"gave up: {ex.GetType().Name}, server saw {server.Attempts} requests");
        }
    }
}
