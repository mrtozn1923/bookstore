using Bookstore.Api;
using Bookstore.Catalog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();

WebApplication app = builder.Build();
app.MapBookEndpoints();
app.Run();
