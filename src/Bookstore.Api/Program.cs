using Bookstore.Infrastructure;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Shipping;
using Bookstore.Shipping.LegacyCarrier;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<ExternalServiceExceptionHandler>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddScoped<CheckoutService>();

builder.Services.AddHttpClient<IPaymentGateway, ProviderPaymentGateway>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Payments:Provider:BaseUrl"]!);
    client.DefaultRequestHeaders.Authorization = new("Bearer", builder.Configuration["Payments:Provider:ApiKey"]);
});

builder.Services.AddHttpClient<IShippingCarrier, LegacyCarrierClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Shipping:LegacyCarrier:BaseUrl"]!);
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("/api/orders/{id:int}/checkout", async (int id, CheckoutRequest request, CheckoutService checkout, CancellationToken ct) =>
    await checkout.CheckoutAsync(id, request.CardToken, ct) is { } result
        ? Results.Ok(result)
        : Results.NotFound());

app.Run();

public sealed record CheckoutRequest(string CardToken);
