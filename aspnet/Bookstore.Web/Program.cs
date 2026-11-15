using Bookstore.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();

WebApplication app = builder.Build();
app.MapStaticAssets();
app.MapControllerRoute("default", "{controller=Catalog}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
