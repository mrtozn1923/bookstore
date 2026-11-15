using Bookstore.Web.Catalog;
using Bookstore.Web.Errors;
using Bookstore.Web.Ordering;
using Bookstore.Web.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection("Catalog"));
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();
builder.Services.AddScoped<CartService>();

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
    TimeoutException => StatusCodes.Status503ServiceUnavailable,
    _ => StatusCodes.Status500InternalServerError
};

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

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
