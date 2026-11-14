using Bookstore.IO.Json;
using Bookstore.Ordering;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Source-generated metadata first; types it does not know fall back to the next resolver.
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, BookstoreJsonContext.Default);
});

var app = builder.Build();

// Streams a JSON array: each order is serialized as it is produced, nothing is buffered.
app.MapGet("/api/orders/export", (int count) => ExportOrders(count));

app.MapGet("/api/orders/summary", (int count) =>
{
    var orders = SampleOrders.Generate(count).ToList();
    return new ExportSummary(orders.Count, orders.Sum(o => o.Total), orders.Select(o => o.CustomerName).Distinct().Count());
});

app.Run();

static async IAsyncEnumerable<Order> ExportOrders(int count)
{
    foreach (var order in SampleOrders.Generate(count))
    {
        yield return order;
    }
    await Task.CompletedTask;
}
