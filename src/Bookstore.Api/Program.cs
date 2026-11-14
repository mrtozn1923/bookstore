using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Configuration;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Observability;
using Bookstore.Ordering;
using Bookstore.Payments;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// Incident levers (kill switch, payment timeout) live in their own small file that is watched for changes.
// Added last, so it wins over appsettings*.json and environment variables. See docs/incidents/levers.md.
builder.AddSwitchesFile();
builder.AddBookstoreObservability();

string connectionString = builder.Configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException(
        "Connection string 'BookstoreDb' is missing. Set it with user-secrets or the ConnectionStrings__BookstoreDb environment variable (see README.md).");

builder.Services.AddDbContext<BookstoreContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule()
    .AddPaymentsModule();

WebApplication app = builder.Build();
// From here on a broken switches.json is not ignored any more: running instances keep their last values.
app.Lifetime.ApplicationStarted.Register(SwitchesFile.MarkStarted);

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

app.MapCatalogEndpoints();
app.MapOrderingEndpoints();
app.MapInventoryEndpoints();
// For the load balancer and Kubernetes probes; not a user journey, so it is left out of the SLIs.
app.MapHealthChecks("/health");

app.Run();
