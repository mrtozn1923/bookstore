using Bookstore.Infrastructure;
using Bookstore.Notifications;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Payments.ExchangeRates;
using Bookstore.Shipping;
using Bookstore.Shipping.LegacyCarrier;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

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

// Payments: POST is not idempotent yet (post 06), so no retries; short timeouts from appsettings.
var payments = builder.Services.AddHttpClient(ProviderPaymentGateway.ClientName, client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Payments:Provider:BaseUrl"]!);
        client.DefaultRequestHeaders.Authorization = new("Bearer", builder.Configuration["Payments:Provider:ApiKey"]);
    })
    .AddAsKeyed();
payments.AddStandardResilienceHandler()
    .Configure(builder.Configuration.GetSection("Resilience:PaymentProvider"))
    .Configure(options => options.Retry.DisableForUnsafeHttpMethods());
builder.Services.AddScoped<IPaymentGateway, ProviderPaymentGateway>();

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
});
// Added after (inside) the resilience handler: every retry asks for the current token.
carrier.AddHttpMessageHandler<CarrierAuthHandler>();
builder.Services.AddKeyedScoped<IShippingCarrier, LegacyCarrierClient>(ResilientShippingCarrier.InnerKey);
builder.Services.AddScoped<IShippingCarrier, ResilientShippingCarrier>();
builder.Services.AddResiliencePipeline<string, ShippingQuote>(ResilientShippingCarrier.PipelineName, (pipeline, context) =>
    ResilientShippingCarrier.Configure(pipeline,
        context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<ResilientShippingCarrier>()));
builder.Services.AddHealthChecks().AddCheck<CarrierCircuitHealthCheck>("legacy-carrier");

builder.Services.AddNotifications(builder.Configuration);

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
    await checkout.CheckoutAsync(id, request.CardToken, ct) is { } result
        ? Results.Ok(result)
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
