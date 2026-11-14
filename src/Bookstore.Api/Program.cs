using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Notifications;
using Bookstore.Observability;
using Bookstore.Ordering;
using Bookstore.Payments;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss.fff ";
    options.UseUtcTimestamp = true;
});
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
// bookstore-web served from another origin: the browser may start the trace, so it must be allowed to send it.
builder.Services.AddCors(options => options.AddPolicy("bookstore-web", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("content-type", "traceparent", "tracestate")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule()
    .AddNotificationsModule()
    .AddPaymentsModule(builder.Configuration);

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("bookstore-web");

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
// Prometheus scrapes this; in production keep it on an internal port or behind authorization.
app.MapPrometheusScrapingEndpoint();

app.Run();
