WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Build'den önce okunan bir ayar: 2. yazıdaki sorunun aynısı
string searchMode = builder.Configuration["Catalog:SearchMode"] ?? "contains";

WebApplication app = builder.Build();
app.MapGet("/search-mode", () => searchMode);
app.Run();
