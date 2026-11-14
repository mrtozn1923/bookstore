using Microsoft.AspNetCore.Http.Features;
using Bookstore.IO.Compression;
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

// Tüm faturalar tek zip'te: disk ya da MemoryStream yok, entry'ler üretildikçe response'a gider.
app.MapGet("/api/invoices/zip", async (int count, HttpContext context) =>
{
    // .NET 10'da her entry'nin son birkaç byte'ı senkron yazılır (dotnet/runtime#121624, .NET 11'de düzeldi).
    context.Features.GetRequiredFeature<IHttpBodyControlFeature>().AllowSynchronousIO = true;
    context.Response.ContentType = "application/zip";
    context.Response.Headers.ContentDisposition = "attachment; filename=\"invoices.zip\"";
    await InvoiceZip.WriteAsync(context.Response.Body, SampleOrders.Generate(count), context.RequestAborted);
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
