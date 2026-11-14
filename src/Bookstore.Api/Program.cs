using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Bookstore.Payments;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException(
        "Connection string 'BookstoreDb' is missing. Set it with user-secrets or the ConnectionStrings__BookstoreDb environment variable (see README.md).");

builder.Services.AddDbContext<BookstoreContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOutputCache();

// Optional W3C access log (one line per request); used to derive the traffic mix for load tests.
bool accessLogEnabled = builder.Configuration.GetValue<bool>("AccessLog:Enabled");
if (accessLogEnabled)
{
    builder.Services.AddW3CLogging(options =>
    {
        options.LoggingFields = W3CLoggingFields.Date | W3CLoggingFields.Time | W3CLoggingFields.Method
            | W3CLoggingFields.UriStem | W3CLoggingFields.UriQuery | W3CLoggingFields.ProtocolStatus
            | W3CLoggingFields.TimeTaken;
        options.LogDirectory = builder.Configuration["AccessLog:Directory"] ?? "logs";
        options.FlushInterval = TimeSpan.FromSeconds(2);
    });
}

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddPaymentsModule(builder.Configuration)
    .AddOrderingModule();

WebApplication app = builder.Build();

if (accessLogEnabled)
{
    app.UseW3CLogging();
}
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Development turns this on in appsettings.Development.json; the load-test compose file turns it on with an environment variable.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.MigrateDatabaseAsync();
}

app.UseOutputCache();

app.MapCatalogEndpoints();
app.MapOrderingEndpoints();

app.Run();
