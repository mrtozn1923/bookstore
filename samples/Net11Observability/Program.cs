using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// .NET 11: what ASP.NET Core and MemoryCache now emit by themselves, without instrumentation packages.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// MemoryCache publishes dotnet.cache.* metrics once statistics are on.
builder.Services.AddMemoryCache(options => options.TrackStatistics = true);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        // No OpenTelemetry.Instrumentation.AspNetCore: the server activity already carries the HTTP attributes.
        .AddSource("Microsoft.AspNetCore", "Bookstore.*")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Microsoft.Extensions.Caching.Memory.MemoryCache")
        .AddConsoleExporter());

WebApplication app = builder.Build();

app.MapGet("/api/books/{isbn}", (string isbn, IMemoryCache cache) =>
    cache.GetOrCreate(isbn, _ =>
    {
        // Only a cache miss loads the book (and gets a span).
        using Activity? activity = BookstoreTelemetry.Catalog.StartActivity("load book");
        activity?.SetTag("bookstore.book.isbn", isbn);
        return new BookView(isbn, "Dune");
    }));

app.Run();

internal sealed record BookView(string Isbn, string Title);
