using System.Net.Http.Json;
using Bookstore.FakeProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bookstore.ResilienceLab.Demos;

public static class FactoryDemos
{
    private const string FakeToken = "fake-carrier-token-demo";

    // How long does a handler chain live, and which primary handler does the factory build?
    public static async Task LifetimeAsync()
    {
        using var carrier = FakeLegacyCarrier.Start();
        var services = new ServiceCollection();
        var chains = 0;
        services.AddHttpClient("carrier", c => c.BaseAddress = new Uri(carrier.Url!))
            .SetHandlerLifetime(TimeSpan.FromSeconds(2))
            .ConfigurePrimaryHttpMessageHandler((handler, _) =>
            {
                var sockets = (SocketsHttpHandler)handler;
                Console.WriteLine($"  new chain #{++chains}: {handler.GetType().Name}, PooledConnectionLifetime={sockets.PooledConnectionLifetime}");
            });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        var start = DateTime.UtcNow;
        foreach (var second in new[] { 0.0, 1.0, 2.5, 3.0, 5.0 })
        {
            await Task.Delay(start.AddSeconds(second) - DateTime.UtcNow is { Ticks: > 0 } wait ? wait : TimeSpan.Zero);
            var client = factory.CreateClient("carrier");
            Console.WriteLine($"t={second:0.0}s CreateClient -> {client.GetHashCode()}");
        }
    }

    public sealed class QuoteService(HttpClient httpClient)
    {
        public HttpClient Client => httpClient;
    }

    public sealed class PriceCache(QuoteService quotes)
    {
        public QuoteService Quotes => quotes;
    }

    public sealed class KeyedPriceCache([FromKeyedServices("carrier")] HttpClient client)
    {
        public HttpClient Client => client;
    }

    // Typed client in a singleton: silent. Keyed (scoped) client in a singleton: an error at resolve time.
    public static Task CaptiveAsync()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<QuoteService>();
        services.AddSingleton<PriceCache>();
        services.AddHttpClient("carrier").AddAsKeyed();
        services.AddSingleton<KeyedPriceCache>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        var cache = provider.GetRequiredService<PriceCache>();
        Console.WriteLine($"Typed client captured by singleton: {cache.Quotes.Client.GetHashCode()} (no error)");
        try
        {
            provider.GetRequiredService<KeyedPriceCache>();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Keyed client in singleton: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public sealed class RequestContext
    {
        public string Name { get; set; } = "unset";
    }

    public sealed class ScopeProbeHandler(RequestContext context) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  handler sees RequestContext.Name = {context.Name}");
            return base.SendAsync(request, cancellationToken);
        }
    }

    // A handler's scoped dependency comes from the handler's own scope, not from the request scope.
    public static async Task ScopeAsync()
    {
        using var carrier = FakeLegacyCarrier.Start();
        var services = new ServiceCollection();
        services.AddScoped<RequestContext>();
        services.AddTransient<ScopeProbeHandler>();
        services.AddHttpClient("carrier", c =>
            {
                c.BaseAddress = new Uri(carrier.Url!);
                c.DefaultRequestHeaders.Authorization = new("Bearer", FakeToken);
            })
            .AddHttpMessageHandler<ScopeProbeHandler>()
            .AddAsKeyed();
        await using var provider = services.BuildServiceProvider();

        foreach (var customer in new[] { "Ada", "Alan" })
        {
            await using var scope = provider.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<RequestContext>().Name = customer;
            var client = scope.ServiceProvider.GetRequiredKeyedService<HttpClient>("carrier");
            Console.WriteLine($"Request scope for {customer}:");
            using var _ = await client.GetAsync("api/v2/quotes?from=34000&to=06100&weight=850");
        }
    }

    // What does the factory log, and what does it hide?
    public static async Task LoggingAsync()
    {
        using var carrier = FakeLegacyCarrier.Start();
        foreach (var redactOnlyAuthorization in new[] { false, true })
        {
            Console.WriteLine(redactOnlyAuthorization ? "--- RedactLoggedHeaders(Authorization)" : "--- default");
            var services = new ServiceCollection();
            services.AddLogging(b => b
                .AddSimpleConsole(o => o.SingleLine = false)
                .SetMinimumLevel(LogLevel.Warning)
                .AddFilter("System.Net.Http.HttpClient.carrier.ClientHandler", LogLevel.Trace));
            var http = services.AddHttpClient("carrier", c =>
            {
                c.BaseAddress = new Uri(carrier.Url!);
                c.DefaultRequestHeaders.Authorization = new("Bearer", FakeToken);
                c.DefaultRequestHeaders.Add("X-Correlation-ID", "4bf92f3577b34da6a3ce929d0e0e4736");
            });
            if (redactOnlyAuthorization)
            {
                http.RedactLoggedHeaders(["Authorization"]);
            }

            await using (var provider = services.BuildServiceProvider())
            {
                var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("carrier");
                using var _ = await client.GetAsync("api/v2/quotes?from=34000&to=06100&weight=850");
            }
        }
    }

    // HttpClient.Timeout and a CancellationToken both end in TaskCanceledException; the inner exception tells them apart.
    public static async Task TimeoutAsync()
    {
        using var carrier = FakeLegacyCarrier.Start(mode: FakeMode.Slow);
        var client = new HttpClient { BaseAddress = new Uri(carrier.Url!), Timeout = TimeSpan.FromSeconds(2) };
        client.DefaultRequestHeaders.Authorization = new("Bearer", FakeToken);
        Console.WriteLine($"Default HttpClient.Timeout: {new HttpClient().Timeout}");

        try
        {
            await client.GetAsync("api/v2/quotes?from=34000&to=06100&weight=850");
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Timeout     : {ex.GetType().Name}, inner={ex.InnerException?.GetType().Name ?? "null"}");
            Console.WriteLine($"              {ex.Message}");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await client.GetAsync("api/v2/quotes?from=34000&to=06100&weight=850", cts.Token);
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Caller token: {ex.GetType().Name}, inner={ex.InnerException?.GetType().Name ?? "null"}, " +
                              $"token cancelled={ex.CancellationToken == cts.Token}");
            Console.WriteLine($"              {ex.Message}");
        }
    }

    public sealed class StampHandler : DelegatingHandler;

    // A DelegatingHandler registered as a singleton breaks as soon as the factory builds a second chain.
    public static Task SingletonHandlerAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<StampHandler>();
        services.AddHttpClient("payments").AddHttpMessageHandler<StampHandler>();
        services.AddHttpClient("carrier").AddHttpMessageHandler<StampHandler>();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        factory.CreateClient("payments");
        Console.WriteLine("payments: chain built");
        try
        {
            factory.CreateClient("carrier");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"carrier : {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public static Task BaseAddressAsync()
    {
        foreach (var (baseUrl, path) in new[]
                 {
                     ("http://localhost:9092/api/v2", "quotes"),
                     ("http://localhost:9092/api/v2/", "quotes"),
                     ("http://localhost:9092/api/v2/", "/quotes")
                 })
        {
            Console.WriteLine($"{baseUrl,-30} + {path,-8} -> {new Uri(new Uri(baseUrl), path)}");
        }

        return Task.CompletedTask;
    }

    public sealed record TrackingEvent(string CODE, DateTimeOffset AT, string PLACE);

    // ResponseHeadersRead + streaming deserialization: items arrive one by one, the body is never buffered.
    public static async Task StreamAsync()
    {
        using var carrier = FakeLegacyCarrier.Start();
        using var client = new HttpClient { BaseAddress = new Uri(carrier.Url!) };
        await foreach (var e in client.GetFromJsonAsAsyncEnumerable<TrackingEvent>("api/v2/shipments/TRK-48213/events"))
        {
            Console.WriteLine($"{e!.AT:u} {e.CODE} {e.PLACE}");
        }
    }
}
