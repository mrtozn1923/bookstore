using System.Globalization;
using System.Text.Json.Serialization;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Mvc;

// Prices and dates are formatted for Turkey
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new CheckoutService(new Dictionary<string, Coupon>
{
    ["KITAP10"] = new("KITAP10", 10m, 200m, MaxDiscount: 100m),
}));
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

WebApplication app = builder.Build();

app.MapPost("/api/checkout", (
    CheckoutCommand command,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    CheckoutService checkout) =>
{
    CheckoutResult result = checkout.PlaceOrder(command, idempotencyKey);
    return result.Order is { } order
        ? Results.Created($"/api/orders/{order.OrderId}", order)
        : Results.Problem(detail: result.Error, statusCode: StatusCodes.Status422UnprocessableEntity);
});

app.Run();
