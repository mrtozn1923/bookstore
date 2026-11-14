using Bookstore.Api.Catalog;
using Bookstore.Api.Rum;
using Microsoft.Extensions.FileProviders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // The Angular build output is served by this API (same origin for /api calls)
    WebRootPath = "../../bookstore-web/dist/bookstore-web/browser",
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<BookCatalog>();
builder.Services.AddSingleton<RumStore>();

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Covers")),
    RequestPath = "/covers",
});

app.MapCatalogEndpoints();
app.MapRumEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
