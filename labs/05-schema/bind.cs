#:package Microsoft.Extensions.Configuration.Json@10.0.15
#:package Microsoft.Extensions.Configuration.Binder@10.0.15
#:property PublishAot=false
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile(Path.GetFullPath("../../bookstore/src/Bookstore.Api/appsettings.json"))
    .AddJsonFile(Path.GetFullPath("appsettings.Staging.json"))
    .Build();

// default binding: unknown keys are ignored, "100" is converted to int
CatalogOptions catalog = config.GetSection("Catalog").Get<CatalogOptions>()!;
Console.WriteLine($"PageSize={catalog.PageSize}, MaxPageSize={catalog.MaxPageSize}");

// the binder can reject keys that have no matching property
try
{
    config.GetSection("Catalog").Get<CatalogOptions>(o => o.ErrorOnUnknownConfiguration = true);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

class CatalogOptions
{
    public int PageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
}
