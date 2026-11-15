using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Institutional;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Bookstore.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException(
        "Connection string 'BookstoreDb' is missing. Set it in appsettings.Development.json or the ConnectionStrings__BookstoreDb environment variable (see README.md).");

// SQLite has no schemas: the module schemas (catalog, ordering, inventory) are ignored on purpose.
builder.Services.AddDbContext<BookstoreContext>(options => options
    .UseSqlite(connectionString)
    .ConfigureWarnings(w => w.Ignore(SqliteEventId.SchemaConfiguredWarning)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Tokens come from the identity provider (Authentication:Schemes:Bearer in configuration).
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule()
    .AddInstitutionalModule();

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
app.MapInstitutionalEndpoints();
app.MapSecurityTxt();

app.Run();
