using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException(
        "Connection string 'BookstoreDb' is missing. Set it with user-secrets or the ConnectionStrings__BookstoreDb environment variable (see README.md).");

// SQL Server is the default. The GitOps series runs PostgreSQL inside the cluster (Database:Provider=Postgres);
// its migrations live in the Bookstore.Migrations.Postgres assembly.
string provider = builder.Configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
builder.Services.AddDbContext<BookstoreContext>(options => _ = provider switch
{
    "SqlServer" => options.UseSqlServer(connectionString),
    "Postgres" => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Bookstore.Migrations.Postgres")),
    _ => throw new InvalidOperationException($"Unknown Database:Provider '{provider}'. Use SqlServer or Postgres."),
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookstoreContext>("database", tags: ["ready"]);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        await app.MigrateDatabaseAsync();
    }
}

// Liveness only says the process answers; readiness also needs the database.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/api/info", (IHostEnvironment env) => new AppInfo(
    Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev",
    env.EnvironmentName,
    Environment.MachineName,
    provider));

app.MapCatalogEndpoints();
app.MapOrderingEndpoints();

app.Run();

internal sealed record AppInfo(string Version, string Environment, string Pod, string Database);
