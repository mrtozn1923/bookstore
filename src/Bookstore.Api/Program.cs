using Bookstore.Api;
using Bookstore.Contracts;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var appVersion = AppVersion.FromAssembly(typeof(Program).Assembly);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("bookstore-api", serviceVersion: appVersion.Version))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

app.Logger.LogInformation("Bookstore.Api {Version} ({Commit}) starting", appVersion.Version, appVersion.Commit);

BookDto[] books =
[
    new("9780441172719", "Dune", "Frank Herbert", new(420.00m, "TRY")),
    new("9780141439587", "Emma", "Jane Austen", new(185.50m, "TRY")),
    new("9780547928227", "The Hobbit", "J. R. R. Tolkien", new(310.00m, "TRY")),
];

app.MapGet("/api/books", (decimal? maxPrice) =>
    books.Where(book => maxPrice is null || book.ListPrice.Amount <= maxPrice));
app.MapGet("/version", () => appVersion);

app.Run();
