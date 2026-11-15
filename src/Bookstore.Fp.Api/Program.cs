using System.Globalization;
using Bookstore.Fp.Api;
using Bookstore.Ordering.Orders;
using Bookstore.Ordering.Pricing;

// Same number format in the log on every machine
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

TimeSpan istanbul = TimeSpan.FromHours(3);
Dictionary<string, CatalogBook> catalog = new()
{
    ["9780441013593"] = new("9780441013593", "Dune", 189.50m, IsEbook: false),
    ["9780141439587"] = new("9780141439587", "Emma", 59.90m, IsEbook: true)
};
int next = 41;
builder.Services.AddSingleton(new OrderPlacement(
    catalog,
    new InMemoryStock(new() { ["9780441013593"] = 3, ["9780141439587"] = 100 }),
    new Campaign("AUTUMN10", 0.10m, new(2026, 11, 1, 0, 0, 0, istanbul), new(2026, 12, 1, 0, 0, 0, istanbul)),
    TimeProvider.System,
    () => $"BS-2026-{Interlocked.Increment(ref next):D4}",
    Console.Out));

WebApplication app = builder.Build();

app.MapPost("/orders", async (PlaceOrderRequest request, OrderPlacement placement, CancellationToken ct) =>
    (await placement.PlaceAsync(request, ct)).ToHttpResult());

app.Run();
