#:package Microsoft.Extensions.Configuration.Json@10.0.15
#:package Microsoft.Extensions.Configuration.Binder@10.0.15
#:property PublishAot=false
using System.Text.Json;
using Microsoft.Extensions.Configuration;

string path = Path.GetFullPath("../../bookstore/src/Bookstore.Api/appsettings.json");

// the configuration provider skips comments and accepts the trailing comma
IConfiguration config = new ConfigurationBuilder().AddJsonFile(path).Build();
CatalogOptions catalog = config.GetSection("Catalog").Get<CatalogOptions>()!;
Console.WriteLine($"Configuration: PageSize={catalog.PageSize}, MaxPageSize={catalog.MaxPageSize}");

// the serializer with default options does not
try
{
    JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));
}
catch (JsonException ex)
{
    Console.WriteLine($"Serializer: {ex.Message}");
}

// the same options the provider uses
JsonDocumentOptions jsonc = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path), jsonc);
Console.WriteLine($"JsonDocument + options: {doc.RootElement.GetProperty("Ordering").GetProperty("InstitutionalDiscountRate")}");

record CatalogOptions(int PageSize, int MaxPageSize);
