using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
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

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule()
    .AddPaymentsModule(builder.Configuration);

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

app.MapCatalogEndpoints();
app.MapOrderingEndpoints();
app.MapInventoryEndpoints();

app.Run();
