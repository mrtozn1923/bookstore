using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Identity;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Mobile series: SQLite instead of the reference app's SQL Server, so the API runs without a container.
builder.Services.AddDbContext<BookstoreContext>(options => options
    .UseSqlite(builder.Configuration.GetConnectionString("BookstoreDb"))
    // SQLite has no schemas (catalog, ordering, inventory); EF Core ignores them
    .ConfigureWarnings(w => w.Ignore(SqliteEventId.SchemaConfiguredWarning)));
builder.Services.AddDbContext<AuthContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AuthDb")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Sign-in for the mobile app: ASP.NET Core Identity's ready-made endpoints (/login, /refresh, /register).
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AuthContext>();
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
    options.BearerTokenExpiration = TimeSpan.FromMinutes(builder.Configuration.GetValue("Auth:AccessTokenMinutes", 60.0)));

// Only for the Flutter web build during development; a mobile app does not need CORS.
string[] devOrigins = builder.Configuration.GetSection("Cors:DevOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("dev", policy => policy.WithOrigins(devOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services
    .AddCatalogModule()
    .AddInventoryModule()
    .AddOrderingModule();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
    app.MapOpenApi();
    if (app.Configuration.GetValue<bool>("Database:CreateOnStartup"))
    {
        await app.CreateDatabasesAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").WithTags("Auth").MapIdentityApi<IdentityUser>();
app.MapCatalogEndpoints();
app.MapOrderingEndpoints();

app.Run();
