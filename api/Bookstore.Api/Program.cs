using Bookstore.Api.Catalog;
using Bookstore.Api.Images;
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
builder.Services.AddMemoryCache(options => options.SizeLimit = 64 * 1024 * 1024);
builder.Services.AddSingleton<CoverImageService>();
// Compress text responses (HTML, JS, CSS, JSON); the Brotli/zstd details are in the navigation post
builder.Services.AddResponseCompression();

WebApplication app = builder.Build();

app.UseResponseCompression();
// "/" -> index.html, the home page prerendered at build time
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Covers")),
    RequestPath = "/covers",
});
// Route matching after the static files: otherwise the fallback endpoint below already
// matches "/" and UseDefaultFiles never serves the prerendered index.html
app.UseRouting();

app.MapCatalogEndpoints();
app.MapRumEndpoints();
app.MapCoverImageEndpoints();
// Every other route renders on the client: serve the CSR shell, not the prerendered home page
app.MapFallbackToFile("index.csr.html");

app.Run();
