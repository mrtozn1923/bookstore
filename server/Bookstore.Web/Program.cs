using Bookstore.Web.Catalog;
using Bookstore.Web.Vite;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<ViteOptions>(builder.Configuration.GetSection(ViteOptions.SectionName));
builder.Services.AddSingleton<ViteManifest>();
builder.Services.AddSingleton<BookCatalog>();

WebApplication app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Hashed files never change: cache them for a year
        if (context.Context.Request.Path.StartsWithSegments("/dist/assets"))
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    },
});

app.MapGet("/api/books", (BookCatalog catalog) => catalog.GetAll());
app.MapDefaultControllerRoute();

app.Run();
