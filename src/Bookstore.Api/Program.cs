using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database:Provider = SqlServer (default) | InMemory (demos and chart tests without a database server).
string provider = builder.Configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<BookstoreContext>(options => options.UseInMemoryDatabase("BookstoreDb"));
}
else
{
    string connectionString = builder.Configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException(
            "Connection string 'BookstoreDb' is missing. Set it with user-secrets or the ConnectionStrings__BookstoreDb environment variable (see README.md).");
    builder.Services.AddDbContext<BookstoreContext>(options => options.UseSqlServer(connectionString));
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookstoreContext>("database", tags: ["ready"]);

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    // The in-memory store is empty at start; EnsureCreated applies the HasData seed (20 books).
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.EnsureCreatedAsync();
}
else if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.MigrateDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness: the process answers. Readiness: the database answers too.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapGet("/api/info", (IConfiguration config, IHostEnvironment env) => new
{
    version = config["APP_VERSION"] ?? "dev",
    environment = env.EnvironmentName,
    pod = Environment.MachineName,
    database = provider,
    storeName = config["Bookstore:StoreName"] ?? "Bookstore",
}).WithTags("Info");

app.MapCatalogEndpoints();
app.MapOrderingEndpoints();

app.Run();
