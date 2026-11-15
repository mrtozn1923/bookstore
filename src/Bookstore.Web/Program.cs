using Bookstore.Web.Catalog;
using Bookstore.Web.Errors;
using Bookstore.Web.Institutional;
using Bookstore.Web.Ordering;
using Bookstore.Web.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection("Catalog"));
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<InstitutionalOrderService>();
builder.Services.AddHttpContextAccessor();

// State across requests
builder.Services.AddSingleton<ICartStore, CookieCartStore>();
builder.Services.Configure<CookieTempDataProviderOptions>(options => options.Cookie.IsEssential = true);

string? redis = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redis))
{
    builder.Services.AddDistributedMemoryCache(); // one instance only: data lives in this process
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redis;
        options.InstanceName = "bookstore-web:";
    });
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".Bookstore.Session";
    options.Cookie.Path = "/institutional-orders"; // only the wizard's requests carry it (and touch the store)
    options.Cookie.IsEssential = true; // the institutional order form can't work without it
});

// Every instance must read the others' cookies: same application name, shared key ring
IDataProtectionBuilder dataProtection = builder.Services.AddDataProtection().SetApplicationName("Bookstore.Web");
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = _ => true; // non-essential cookies wait for consent
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddAuthentication(DemoHeaderAuthenticationHandler.SchemeName)
    .AddScheme<DemoHeaderOptions, DemoHeaderAuthenticationHandler>(DemoHeaderAuthenticationHandler.SchemeName,
        options => options.Enabled = builder.Configuration.GetValue<bool>("Demo:HeaderAuth"));
builder.Services.AddAuthorization();

WebApplication app = builder.Build();

static bool IsApi(HttpContext context) => context.Request.Path.StartsWithSegments("/api");

static int StatusFor(Exception exception) => exception switch
{
    DomainException domain => domain.StatusCode,
    TimeoutException or StackExchange.Redis.RedisConnectionException => StatusCodes.Status503ServiceUnavailable,
    _ => StatusCodes.Status500InternalServerError
};

app.UseCookiePolicy();

// /api: JSON ProblemDetails
app.UseWhen(IsApi, api =>
{
    api.UseExceptionHandler(new ExceptionHandlerOptions { StatusCodeSelector = StatusFor });
    api.UseStatusCodePages();
});

// Pages: HTML error views. In Development the developer exception page shows unhandled exceptions instead.
app.UseWhen(context => !IsApi(context), web =>
{
    if (!app.Environment.IsDevelopment())
    {
        web.UseExceptionHandler(new ExceptionHandlerOptions
        {
            ExceptionHandlingPath = "/error",
            StatusCodeSelector = StatusFor
        });
    }
    web.UseStatusCodePagesWithReExecute("/error/{0}");
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Session only where it is used: every request through UseSession refreshes its entry in the store
app.UseWhen(context => context.Request.Path.StartsWithSegments("/institutional-orders"), wizard => wizard.UseSession());

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
