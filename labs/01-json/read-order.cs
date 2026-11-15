#:package Microsoft.Extensions.Configuration.Json@10.0.15
#:property PublishAot=false
using System.Text.Json;
using Microsoft.Extensions.Configuration;

string json = File.ReadAllText("order.json");

// 1) System.Text.Json with the web defaults
Order order = JsonSerializer.Deserialize<Order>(json, JsonSerializerOptions.Web)!;
Console.WriteLine($"Serializer: {order.OrderId} {order.Status} {order.Total}");

// 2) The same with duplicates rejected (.NET 10)
JsonSerializerOptions strict = new(JsonSerializerOptions.Web) { AllowDuplicateProperties = false };
try
{
    JsonSerializer.Deserialize<Order>(json, strict);
}
catch (JsonException ex)
{
    Console.WriteLine($"Strict: {ex.Message}");
}

// 3) Configuration JSON provider (appsettings.json is read this way)
try
{
    IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("order.json")
        .Build();
    Console.WriteLine($"Configuration: {config["status"]}");
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"Configuration: {ex.InnerException?.Message}");
}

record Order(long OrderId, string Status, decimal Total);
