using Bookstore.Infrastructure;
using Bookstore.Notifications;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Payments.ExchangeRates;
using Bookstore.Payments.Webhooks;
using Bookstore.Shipping;
using Bookstore.Shipping.LegacyCarrier;
using Bookstore.Webhooks;
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
// Incoming payment webhooks: verify, store, acknowledge; processing happens in the background.
builder.Services.Configure<PaymentWebhookOptions>(builder.Configuration.GetSection("Payments:Provider:Webhooks"));
builder.Services.AddSingleton<PaymentWebhookInbox>();
builder.Services.AddHostedService<PaymentWebhookProcessor>();

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

// Outgoing webhooks to institutional customers: outbox + background delivery.
builder.Services.AddSingleton<WebhookSubscriptionStore>();
builder.Services.AddSingleton<WebhookOutbox>();
builder.Services.AddSingleton<WebhookPublisher>();
builder.Services.AddScoped<ShipOrderService>();
builder.Services.Configure<WebhookDeliveryOptions>(builder.Configuration.GetSection("Webhooks:Delivery"));
builder.Services.AddHostedService<WebhookDispatcher>();
// Someone else's server: no redirects, short connect timeout, 15 s per attempt, no retries here
// (the outbox schedule is the retry). SSRF filtering of the target address: API Security series, post 08.
builder.Services.AddHttpClient(WebhookDispatcher.ClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    })
    .AddResilienceHandler("webhooks", pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(15)));

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

app.MapPost("/api/orders/{id:int}/ship", (int id, ShipRequest request, ShipOrderService shipping) =>
    shipping.Ship(id, request.TrackingNumber) is { } result ? Results.Ok(result) : Results.NotFound());

// Webhook administration (no auth in this sample; in Bookstore it sits behind the StoreStaff policy).
app.MapPost("/admin/webhooks/subscriptions", (SubscriptionRequest request, WebhookSubscriptionStore store, IHostEnvironment environment) =>
{
    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url)
        || !(url.Scheme == Uri.UriSchemeHttps || (environment.IsDevelopment() && url.IsLoopback)))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["url"] = ["An absolute https URL is required."] });
    }

    var subscription = store.Add(request.InstitutionId, url, request.EventTypes);
    // The secret is shown once, at creation; afterwards only the institution has it.
    return Results.Created($"/admin/webhooks/subscriptions/{subscription.Id}",
        new { subscription.Id, subscription.Url, secret = subscription.Secrets[0].Value });
});
app.MapPost("/admin/webhooks/subscriptions/{id:guid}/rotate-secret", (Guid id, WebhookSubscriptionStore store, TimeProvider time) =>
{
    if (store.Find(id) is not { } subscription)
    {
        return Results.NotFound();
    }

    // The old secret keeps signing for 24 hours, so the institution can switch without losing events.
    var old = subscription.Secrets.Single(s => s.ExpiresAt is null);
    subscription.Secrets[subscription.Secrets.IndexOf(old)] = old with { ExpiresAt = time.GetUtcNow().AddHours(24) };
    var secret = WebhookSignature.NewSecret();
    subscription.Secrets.Add(new WebhookSecret(secret, null));
    return Results.Ok(new { subscription.Id, secret, previousSecretExpiresAt = time.GetUtcNow().AddHours(24) });
});
app.MapGet("/admin/webhooks/messages", (WebhookOutbox outbox) => outbox.All().Select(m => new
{
    m.Id,
    m.EventType,
    m.Status,
    m.NextAttemptAt,
    Attempts = m.Attempts.Select(a => new { a.At, a.StatusCode, a.Error, DurationMs = (int)a.Duration.TotalMilliseconds })
}));
// The "resend" button: same webhook-id, so the customer can still deduplicate.
app.MapPost("/admin/webhooks/messages/{id}/resend", (string id, WebhookOutbox outbox, TimeProvider time) =>
{
    if (outbox.Find(id) is not { } message)
    {
        return Results.NotFound();
    }

    message.Status = WebhookMessageStatus.Pending;
    message.ConsecutiveFailures = 0;
    message.NextAttemptAt = time.GetUtcNow();
    return Results.Accepted();
});

// Anonymous by design: the signature, not a user, proves who is calling.
app.MapPost("/webhooks/payment-provider", PaymentWebhookEndpoint.ReceiveAsync);

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

public sealed record ShipRequest(string TrackingNumber);

public sealed record SubscriptionRequest(string InstitutionId, string Url, string[] EventTypes);
