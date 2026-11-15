using Bookstore.Admin.Components;
using Bookstore.Admin.Client.Catalog;
using Bookstore.Admin.Client.Ordering;
using Bookstore.Admin.Client.Shared;
using Bookstore.Admin.Identity;
using Bookstore.Admin.Services;
using Bookstore.Admin.Validation;
using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

string apiBaseUrl = builder.Configuration["BookstoreApi:BaseUrl"]
    ?? throw new InvalidOperationException("BookstoreApi:BaseUrl is missing (see appsettings.json).");
builder.Services.AddHttpClient<BookstoreApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

// Reads go straight to the database through Bookstore.Data; every write still goes through Bookstore.Api.
string connectionString = builder.Configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException(
        "Connection string 'BookstoreDb' is missing. Set the same one as Bookstore.Api's (user-secrets or ConnectionStrings__BookstoreDb).");
builder.Services.AddDbContextFactory<BookstoreContext>(options => options
    .UseSqlServer(connectionString)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
// Staff accounts (ASP.NET Core Identity, cookie), policies, and the token the API calls carry.
builder.AddPanelIdentity(connectionString);
builder.Services.AddScoped<IBookCatalog, EfBookCatalog>();
builder.Services.AddScoped<SalesReport>();
builder.Services.AddScoped<QuoteState>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<StaffNoteStore>();
// A root-level cascading value reaches every component, whatever its render mode (the WebAssembly side registers its own).
builder.Services.AddCascadingValue(_ => new PanelOptions(Currency: "TRY", AmountFormat: "N2"));
// Source-generated validation metadata: nested objects and collections in [ValidatableType] form models.
builder.Services.AddAdminValidation();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Identity:SeedStaff"))
{
    await app.SeedDevelopmentStaffAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// CSS, scripts and the WebAssembly files must load on the login page too.
app.MapStaticAssets().AllowAnonymous();
app.MapAccountEndpoints();
app.MapPanelCatalogEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bookstore.Admin.Client._Imports).Assembly);

app.Run();
