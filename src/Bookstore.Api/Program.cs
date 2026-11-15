using System.Text.Json;
using Bookstore.Api;
using Bookstore.Catalog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// BOOKSTORE_SEED_FILE comes from the environment (just exports it).
string? seedFile = builder.Configuration["BOOKSTORE_SEED_FILE"];
InMemoryBookCatalog catalog = seedFile is not null && File.Exists(seedFile)
    ? new(JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(seedFile), JsonSerializerOptions.Web) ?? [])
    : new();
builder.Services.AddSingleton<IBookCatalog>(catalog);

WebApplication app = builder.Build();
app.MapBookEndpoints();
app.Run();
