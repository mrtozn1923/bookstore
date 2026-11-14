using System.Globalization;
using Bookstore.Api;
using Microsoft.Extensions.Options;

if (args is ["healthcheck"])
{
    return await HealthProbe.RunAsync();
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// Secrets as files: /run/secrets/ConnectionStrings__Default becomes ConnectionStrings:Default
builder.Configuration.AddKeyPerFile(directoryPath: "/run/secrets", optional: true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.Configure<StoreOptions>(builder.Configuration.GetSection(StoreOptions.SectionName));
builder.Services.AddSingleton<BookCatalog>();

// ConnectionStrings__Default comes from the environment in containers; without it the catalog stays in memory
string? connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton<IBookSource, InMemoryBookSource>();
}
else
{
    builder.Services.AddSingleton<IBookSource>(new SqlBookSource(connectionString));
}

// bookstore-web is served from another origin (its own container), so the browser needs CORS
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).WithMethods("GET").AllowAnyHeader()));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapHealthChecks("/health");
app.MapGet("/books", (BookCatalog catalog, CancellationToken cancellationToken) =>
    catalog.SortedByTitleAsync(cancellationToken));
app.MapGet("/store/clock", (IOptions<StoreOptions> options) =>
{
    TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    DateTimeOffset local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
    return new { zone = zone.Id, localTime = local.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture) };
});

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Environment: {Environment}, GC memory limit: {LimitMb} MB, processors: {Cpus}",
        app.Environment.EnvironmentName,
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024,
        Environment.ProcessorCount));
app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Catalog source: {Source}",
        app.Services.GetRequiredService<IBookSource>().GetType().Name));

await app.RunAsync();
return 0;
