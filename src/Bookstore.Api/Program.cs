using Bookstore.Infrastructure;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Shipping;
using Bookstore.Shipping.LegacyCarrier;
using Microsoft.Extensions.Options;

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

// Payments: keyed client + plain scoped service.
builder.Services.AddHttpClient(ProviderPaymentGateway.ClientName, client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Payments:Provider:BaseUrl"]!);
        client.DefaultRequestHeaders.Authorization = new("Bearer", builder.Configuration["Payments:Provider:ApiKey"]);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddAsKeyed();
builder.Services.AddScoped<IPaymentGateway, ProviderPaymentGateway>();

// Shipping: OAuth token handler on the API client, a separate client for the token endpoint.
builder.Services.AddOptions<LegacyCarrierOptions>()
    .BindConfiguration("Shipping:LegacyCarrier")
    .ValidateOnStart();
builder.Services.AddSingleton<CarrierTokenProvider>();
builder.Services.AddTransient<CarrierAuthHandler>();
builder.Services.AddHttpClient(CarrierTokenProvider.AuthClientName, (sp, client) =>
    client.BaseAddress = sp.GetRequiredService<IOptions<LegacyCarrierOptions>>().Value.BaseUrl);
builder.Services.AddHttpClient(LegacyCarrierClient.ClientName, (sp, client) =>
    {
        client.BaseAddress = sp.GetRequiredService<IOptions<LegacyCarrierOptions>>().Value.BaseUrl;
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddHttpMessageHandler<CarrierAuthHandler>()
    .AddAsKeyed();
builder.Services.AddScoped<IShippingCarrier, LegacyCarrierClient>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("/api/orders/{id:int}/checkout", async (int id, CheckoutRequest request, CheckoutService checkout, CancellationToken ct) =>
    await checkout.CheckoutAsync(id, request.CardToken, ct) is { } result
        ? Results.Ok(result)
        : Results.NotFound());

app.Run();

public sealed record CheckoutRequest(string CardToken);
