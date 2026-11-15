using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database:Provider=InMemory runs the API without SQL Server (Kubernetes lab, demos); seed data comes from HasData.
bool inMemory = string.Equals(builder.Configuration["Database:Provider"], "InMemory", StringComparison.OrdinalIgnoreCase);
if (inMemory)
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

builder.Services.AddHealthChecks();
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

if (inMemory)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.EnsureCreatedAsync();
}

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

app.MapHealthChecks("/health/live");
app.MapCatalogEndpoints();
app.MapOrderingEndpoints();

app.Run();
