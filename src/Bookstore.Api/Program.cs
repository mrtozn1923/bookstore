using System.Globalization;
using Bookstore.Api;
using Microsoft.Extensions.Options;

if (args is ["healthcheck"])
{
    return await HealthProbe.RunAsync();
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.Configure<StoreOptions>(builder.Configuration.GetSection(StoreOptions.SectionName));
builder.Services.AddSingleton<BookCatalog>();

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
app.MapGet("/books", (BookCatalog catalog) => catalog.SortedByTitle());
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

await app.RunAsync();
return 0;
