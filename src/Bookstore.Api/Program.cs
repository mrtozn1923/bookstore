using Bookstore.Infrastructure;
using Bookstore.Notifications;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Payments.ExchangeRates;
using Bookstore.Shipping;
using Bookstore.Shipping.LegacyCarrier;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using System.Net;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<ExternalServiceExceptionHandler>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddSingleton(TimeProvider.System);

// Every client the factory creates gets the correlation header.
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler<CorrelationIdHandler>());

// Payments: every charge carries an Idempotency-Key, so POST may be retried; timeouts from appsettings.
var payments = builder.Services.AddHttpClient(ProviderPaymentGateway.ClientName, client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Payments:Provider:BaseUrl"]!);
        client.DefaultRequestHeaders.Authorization = new("Bearer", builder.Configuration["Payments:Provider:ApiKey"]);
    })
    .AddAsKeyed();
payments.AddStandardResilienceHandler()
    .Configure(builder.Configuration.GetSection("Resilience:PaymentProvider"))
    .Configure(options => options.Retry.ShouldHandle = args =>
        ValueTask.FromResult(ProviderPaymentGateway.ShouldRetry(args.Outcome, args.Context.GetRequestMessage())));
builder.Services.AddScoped<IPaymentGateway, ProviderPaymentGateway>();
builder.Services.AddSingleton<PaymentAttemptStore>();
builder.Services.Configure<PaymentReconciliationOptions>(builder.Configuration.GetSection("Payments:Reconciliation"));
builder.Services.AddHostedService<PaymentReconciliationJob>();

// Shipping: OAuth token handler on the API client, a separate client for the token endpoint.
builder.Services.AddOptions<LegacyCarrierOptions>()
    .BindConfiguration("Shipping:LegacyCarrier")
    .ValidateOnStart();
builder.Services.AddSingleton<CarrierTokenProvider>();
builder.Services.AddTransient<CarrierAuthHandler>();
builder.Services.AddHttpClient(CarrierTokenProvider.AuthClientName, (sp, client) =>
    client.BaseAddress = sp.GetRequiredService<IOptions<LegacyCarrierOptions>>().Value.BaseUrl);
var carrier = builder.Services.AddHttpClient(LegacyCarrierClient.ClientName, (sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IOptions<LegacyCarrierOptions>>().Value.BaseUrl)
    .AddAsKeyed();
// Our own pipeline instead of the standard one: we need the breaker's state and manual control.
builder.Services.AddSingleton<CarrierCircuit>();
carrier.AddResilienceHandler("carrier", (pipeline, context) =>
{
    var circuit = context.ServiceProvider.GetRequiredService<CarrierCircuit>();
    pipeline
        .AddTimeout(TimeSpan.FromSeconds(6))
        .AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(200),
            // MaxDelay does not cap Retry-After, so a long Retry-After (maintenance) is not retried at all.
            ShouldHandle = args => ValueTask.FromResult(
                HttpClientResiliencePredicates.IsTransient(args.Outcome) &&
                !(args.Outcome.Result?.Headers.RetryAfter?.Delta > TimeSpan.FromSeconds(3)))
        })
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
            ManualControl = circuit.ManualControl,
            StateProvider = circuit.StateProvider
        })
        .AddTimeout(TimeSpan.FromSeconds(2));

    // Chaos strategies go last (innermost). Never active in Production, whatever the config says.
    var chaos = context.ServiceProvider.GetRequiredService<IOptionsMonitor<ChaosOptions>>();
    var environment = context.ServiceProvider.GetRequiredService<IHostEnvironment>();
    ValueTask<bool> ChaosEnabled() =>
        ValueTask.FromResult(!environment.IsProduction() && chaos.Get(LegacyCarrierClient.ClientName).Enabled);
    pipeline
        .AddChaosLatency(new ChaosLatencyStrategyOptions
        {
            EnabledGenerator = _ => ChaosEnabled(),
            InjectionRateGenerator = _ => ValueTask.FromResult(chaos.Get(LegacyCarrierClient.ClientName).LatencyRate),
            LatencyGenerator = _ => ValueTask.FromResult(chaos.Get(LegacyCarrierClient.ClientName).Latency)
        })
        .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
        {
            EnabledGenerator = _ => ChaosEnabled(),
            InjectionRateGenerator = _ => ValueTask.FromResult(chaos.Get(LegacyCarrierClient.ClientName).FaultRate),
            OutcomeGenerator = _ => ValueTask.FromResult<Outcome<HttpResponseMessage>?>(
                Outcome.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
        });
});
builder.Services.Configure<ChaosOptions>(LegacyCarrierClient.ClientName, builder.Configuration.GetSection("Chaos:LegacyCarrier"));
// Added after (inside) the resilience handler: every retry asks for the current token.
carrier.AddHttpMessageHandler<CarrierAuthHandler>();
builder.Services.AddKeyedScoped<IShippingCarrier, LegacyCarrierClient>(ResilientShippingCarrier.InnerKey);
builder.Services.AddScoped<IShippingCarrier, ResilientShippingCarrier>();
builder.Services.AddResiliencePipeline<string, ShippingQuote>(ResilientShippingCarrier.PipelineName, (pipeline, context) =>
    ResilientShippingCarrier.Configure(pipeline,
        context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<ResilientShippingCarrier>()));
builder.Services.AddHealthChecks().AddCheck<CarrierCircuitHealthCheck>("legacy-carrier");

builder.Services.AddNotifications(builder.Configuration);

// Polly and HttpClient publish metrics and spans; here they go to the console, in production to an OTLP collector.
if (builder.Configuration.GetValue<bool>("Telemetry:ConsoleExporter"))
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics
            .AddMeter("Polly", "System.Net.Http")
            .AddConsoleExporter((_, reader) => reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10_000))
        .WithTracing(tracing => tracing
            .AddSource("Microsoft.AspNetCore", "System.Net.Http")
            .AddConsoleExporter());
}

// Exchange rates: read-only GET served from two regions, so hedging instead of plain retries.
var rateEndpoints = builder.Configuration.GetSection("ExchangeRates:Endpoints").Get<string[]>()!;
builder.Services.AddHttpClient(ExchangeRateClient.ClientName, client => client.BaseAddress = new Uri(rateEndpoints[0]))
    .AddAsKeyed()
    .AddStandardHedgingHandler(routing => routing.ConfigureOrderedGroups(groups =>
    {
        // One group per region; the hedged attempt goes to the next group.
        foreach (var url in rateEndpoints)
        {
            groups.Groups.Add(new UriEndpointGroup { Endpoints = { new WeightedUriEndpoint { Uri = new Uri(url) } } });
        }
    }))
    .Configure(builder.Configuration.GetSection("Resilience:ExchangeRates"));
builder.Services.AddScoped<ExchangeRateClient>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("/api/orders/{id:int}/checkout", async (int id, CheckoutRequest request, CheckoutService checkout, CancellationToken ct) =>
    await checkout.CheckoutAsync(id, request.CardToken, ct) switch
    {
        null => Results.NotFound(),
        // Outcome unknown: the client polls the order instead of paying again.
        { Status: OrderStatus.PaymentPending } pending => Results.Accepted($"/api/orders/{id}", pending),
        var result => Results.Ok(result)
    });

app.MapGet("/api/orders/{id:int}", (int id, OrderStore orders, PaymentAttemptStore attempts) =>
    orders.Find(id) is { } order
        ? Results.Ok(new
        {
            order.Id,
            order.Status,
            order.ProviderPaymentId,
            Attempts = attempts.ForOrder(id).Select(a => new { a.IdempotencyKey, a.Status })
        })
        : Results.NotFound());

app.MapHealthChecks("/health");

// Maintenance window: hold the carrier circuit open, then close it again.
app.MapPost("/admin/carrier-circuit/isolate", async (CarrierCircuit circuit) =>
{
    await circuit.ManualControl.IsolateAsync();
    return Results.NoContent();
});
app.MapPost("/admin/carrier-circuit/close", async (CarrierCircuit circuit) =>
{
    await circuit.ManualControl.CloseAsync();
    return Results.NoContent();
});

app.MapGet("/api/exchange-rates/{pair}", async (string pair, ExchangeRateClient rates, CancellationToken ct) =>
    Results.Ok(await rates.GetAsync(pair, ct)));

app.Run();

public sealed record CheckoutRequest(string CardToken);
