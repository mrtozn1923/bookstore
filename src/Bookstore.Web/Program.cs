using Bookstore.Web.Catalog;
using Bookstore.Web.Content;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // EN pages live under /en; TR is the default language
    options.Conventions.AddPageRoute("/Index", "en");
    options.Conventions.AddPageRoute("/Blog/Post", "en/blog/{slug}");
    options.Conventions.AddPageRoute("/Authors/Profile", "en/authors/{slug}");
    options.Conventions.AddPageRoute("/Books/Detail", "en/books/{slug}");
});
builder.Services.AddSingleton<PostCatalog>();
builder.Services.AddSingleton<BookCatalog>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PricingService>();

WebApplication app = builder.Build();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

public partial class Program;
