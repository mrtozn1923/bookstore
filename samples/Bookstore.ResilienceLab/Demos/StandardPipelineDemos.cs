using System.Diagnostics;
using Bookstore.FakeProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.ResilienceLab.Demos;

public static class StandardPipelineDemos
{
    public static Task DefaultsAsync()
    {
        var o = new HttpStandardResilienceOptions();
        Console.WriteLine($"RateLimiter       : permits={o.RateLimiter.DefaultRateLimiterOptions.PermitLimit}, queue={o.RateLimiter.DefaultRateLimiterOptions.QueueLimit}");
        Console.WriteLine($"TotalRequestTimeout: {o.TotalRequestTimeout.Timeout}");
        Console.WriteLine($"Retry             : max={o.Retry.MaxRetryAttempts}, backoff={o.Retry.BackoffType}, jitter={o.Retry.UseJitter}, delay={o.Retry.Delay}, retryAfter={o.Retry.ShouldRetryAfterHeader}");
        Console.WriteLine($"CircuitBreaker    : ratio={o.CircuitBreaker.FailureRatio}, minThroughput={o.CircuitBreaker.MinimumThroughput}, sampling={o.CircuitBreaker.SamplingDuration}, break={o.CircuitBreaker.BreakDuration}");
        Console.WriteLine($"AttemptTimeout    : {o.AttemptTimeout.Timeout}");
        return Task.CompletedTask;
    }

    private static ServiceProvider Build(Action<IServiceCollection> configure, LogLevel pollyLevel = LogLevel.Information)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b
            .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss.fff "; })
            .SetMinimumLevel(LogLevel.Warning)
            .AddFilter("Polly", pollyLevel));
        configure(services);
        return services.BuildServiceProvider();
    }

    // GET: two 503s, then 200. The standard handler retries with exponential backoff + jitter.
    public static async Task RetryAsync()
    {
        using var server = new Flaky([503, 503]);
        await using var provider = Build(s => s.AddHttpClient("rates", c => c.BaseAddress = server.Url)
            .AddStandardResilienceHandler());
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("rates");

        var sw = Stopwatch.StartNew();
        using var response = await client.GetAsync("api/rates/EUR-TRY");
        Console.WriteLine($"GET -> {(int)response.StatusCode} after {server.Attempts} attempts in {sw.Elapsed.TotalSeconds:0.0} s");
    }

    // POST: the same 503 with retries disabled for unsafe methods.
    public static async Task PostAsync()
    {
        foreach (var disable in new[] { false, true })
        {
            using var server = new Flaky([503, 503]);
            await using var provider = Build(s => s.AddHttpClient("payments", c => c.BaseAddress = server.Url)
                .AddStandardResilienceHandler(o =>
                {
                    o.Retry.Delay = TimeSpan.FromMilliseconds(100);
                    if (disable)
                    {
                        o.Retry.DisableForUnsafeHttpMethods();
                    }
                }), LogLevel.Warning);
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("payments");
            using var response = await client.PostAsync("v1/charges", new StringContent("{}"));
            Console.WriteLine($"DisableForUnsafeHttpMethods={disable,-5}: POST -> {(int)response.StatusCode}, provider saw {server.Attempts} request(s)");
        }
    }

    // The options are validated when the first client is created.
    public static async Task ValidationAsync()
    {
        await using var provider = Build(s => s.AddHttpClient("carrier")
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
            }));
        try
        {
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("carrier");
        }
        catch (OptionsValidationException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    // The standard handler sets HttpClient.Timeout to infinite; a value set before it is silently overwritten.
    public static async Task ClientTimeoutAsync()
    {
        foreach (var setAfterHandler in new[] { false, true })
        {
            using var server = new Flaky([], delay: TimeSpan.FromSeconds(5));
            await using var provider = Build(s =>
            {
                var http = s.AddHttpClient("carrier", c =>
                {
                    c.BaseAddress = server.Url;
                    c.Timeout = TimeSpan.FromSeconds(3);
                });
                http.AddStandardResilienceHandler(o => o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2));
                if (setAfterHandler)
                {
                    http.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(3));
                }
            }, LogLevel.None);
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("carrier");
            Console.WriteLine($"Timeout set {(setAfterHandler ? "after" : "before")} the handler: HttpClient.Timeout={client.Timeout}");

            var sw = Stopwatch.StartNew();
            try
            {
                await client.GetAsync("api/v2/quotes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {ex.GetType().Name} after {sw.Elapsed.TotalSeconds:0.0} s, server saw {server.Attempts} attempt(s)");
            }
        }
    }

    // Two standard handlers on one client: the retries multiply.
    public static async Task StackedAsync()
    {
        foreach (var handlers in new[] { 1, 2 })
        {
            using var server = new Flaky(Enumerable.Repeat(500, 100).ToArray());
            await using var provider = Build(s =>
            {
                var builder = s.AddHttpClient("carrier", c => c.BaseAddress = server.Url);
                for (var i = 0; i < handlers; i++)
                {
                    builder.AddStandardResilienceHandler(o => o.Retry.Delay = TimeSpan.FromMilliseconds(10));
                }
            }, LogLevel.Warning);
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("carrier");
            using var response = await client.GetAsync("api/v2/quotes");
            Console.WriteLine($"{handlers} handler(s): {(int)response.StatusCode}, server saw {server.Attempts} requests");
        }
    }

    // Hedging: if the primary is slow, a second request goes to the secondary after the hedging delay.
    public static async Task HedgingAsync()
    {
        using var primary = FakeExchangeRates.Start(null, "primary", TimeSpan.FromSeconds(3));
        using var secondary = FakeExchangeRates.Start(null, "secondary", TimeSpan.FromMilliseconds(50));

        await using var provider = Build(s =>
        {
            s.ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler());
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
            s.AddHttpClient("exchange-rates", c => c.BaseAddress = new Uri(primary.Url!))
                .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
                .AddStandardHedgingHandler(routing => routing.ConfigureOrderedGroups(groups =>
                {
                    groups.Groups.Add(new UriEndpointGroup { Endpoints = { new WeightedUriEndpoint { Uri = new Uri(primary.Url!) } } });
                    groups.Groups.Add(new UriEndpointGroup { Endpoints = { new WeightedUriEndpoint { Uri = new Uri(secondary.Url!) } } });
                }))
                .Configure(o => o.Hedging.Delay = TimeSpan.FromMilliseconds(500));
        });
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("exchange-rates");

        var sw = Stopwatch.StartNew();
        var body = await client.GetStringAsync("api/rates/EUR-TRY");
        Console.WriteLine($"{body} in {sw.ElapsedMilliseconds} ms");
    }
}
