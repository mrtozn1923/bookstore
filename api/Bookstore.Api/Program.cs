using System.IO.Compression;
using Bookstore.Api.Catalog;
using Bookstore.Api.Compression;
using Bookstore.Api.Images;
using Bookstore.Api.Rum;
using Bookstore.Api.Ssr;
using Microsoft.AspNetCore.ResponseCompression;
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
// Dynamic responses (JSON, server-rendered HTML) are compressed per request; static files come precompressed
builder.Services.AddResponseCompression(options =>
{
    // Off by default for HTTPS (BREACH): these responses carry no secrets next to user input
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
// Default is Fastest (Brotli quality 1); Optimal is quality 4: much smaller JSON for little CPU
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
// Angular SSR server (Node.js) that renders the pages
builder.Services.AddHttpClient("ssr", client =>
    client.BaseAddress = new Uri(builder.Configuration["Ssr:Origin"] ?? "http://127.0.0.1:5301"));
// Rendered pages are the same for every user (no cookies are forwarded): keep them for a short time
int pageCacheSeconds = builder.Configuration.GetValue("Ssr:CacheSeconds", 60);
builder.Services.AddOutputCache(options =>
    options.AddPolicy("pages", policy => policy.Expire(TimeSpan.FromSeconds(pageCacheSeconds))));

WebApplication app = builder.Build();

app.UseResponseCompression();
// "/" -> index.html, the home page prerendered at build time; "/books/{isbn}" -> books/{isbn}/index.html
// for prerendered book pages, without a 301 redirect to "/books/{isbn}/" (one round trip less)
app.UseDefaultFiles(new DefaultFilesOptions { RedirectToAppendTrailingSlash = false });
app.UsePrecompressedFiles(app.Environment.WebRootFileProvider);
app.UseStaticFiles(PrecompressedFiles.StaticFileOptions());
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Covers")),
    RequestPath = "/covers",
});
// Route matching after the static files: otherwise the fallback endpoint below already
// matches "/" and UseDefaultFiles never serves the prerendered index.html
app.UseRouting();
app.UseOutputCache();

app.MapCatalogEndpoints();
app.MapRumEndpoints();
app.MapCoverImageEndpoints();
// Every other page goes to the Angular SSR server: prerendered, server-rendered or the CSR shell, per route
RouteHandlerBuilder pages = app.MapFallback(SsrProxy.ForwardAsync);
if (pageCacheSeconds > 0)
{
    pages.CacheOutput("pages");
}

app.Run();
