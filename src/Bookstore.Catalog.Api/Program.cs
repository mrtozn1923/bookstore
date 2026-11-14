using Bookstore.Catalog;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Traces and metrics go to an OTLP collector when OTEL_EXPORTER_OTLP_ENDPOINT is set.
OpenTelemetryBuilder otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("bookstore-catalog"))
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());
if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    otel.UseOtlpExporter();
}

InstanceInfo instance = new()
{
    Name = builder.Configuration["Catalog:Instance"] ?? "catalog",
    ResponseDelayMs = builder.Configuration.GetValue<int>("Catalog:ResponseDelayMs"),
};
builder.Services.AddSingleton(instance);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => instance.ForceUnhealthy
        ? HealthCheckResult.Unhealthy("forced by /debug/health")
        : HealthCheckResult.Healthy());

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers[InstanceInfo.Header] = instance.Name;
    if (instance.ResponseDelayMs > 0 && context.Request.Path.StartsWithSegments("/books"))
    {
        await Task.Delay(instance.ResponseDelayMs, context.RequestAborted);
    }
    await next();
});

app.MapHealthChecks("/health");

Book[] books =
[
    new("9780441172719", "Dune", "Frank Herbert", 420.00m),
    new("9780141439587", "Emma", "Jane Austen", 185.50m),
    new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 310.00m),
    new("9780141439549", "Middlemarch", "George Eliot", 265.00m),
];

app.MapGet("/books", () => books);

app.MapGet("/books/summary", () =>
    books.Select(b => new BookSummary(b.Isbn, b.Title)));

app.MapGet("/books/{isbn}", (string isbn) =>
    books.FirstOrDefault(b => b.Isbn == isbn) is { } book
        ? Results.Ok(book)
        : Results.NotFound());

if (app.Environment.IsDevelopment())
{
    // Shows what the gateway actually sent: path, query, Host and X-*/Forwarded headers.
    app.MapGet("/debug/request", (HttpRequest request) => new
    {
        Path = request.Path.Value,
        Query = request.QueryString.Value,
        Host = request.Host.Value,
        Headers = request.Headers
            .Where(h => h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("Forwarded", StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(h => h.Key, h => h.Value.ToString()),
    });

    app.MapPost("/debug/health/{state}", (string state) =>
    {
        instance.ForceUnhealthy = state == "unhealthy";
        return Results.Ok(new { instance.Name, instance.ForceUnhealthy });
    });
}

app.Run();
