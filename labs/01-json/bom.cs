#:property PublishAot=false
using System.Text.Json;

// File.ReadAllText detects and drops the BOM
using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText("stock-bom.json")))
{
    Console.WriteLine($"ReadAllText: stock={doc.RootElement.GetProperty("stock")}");
}

// a stream: the serializer skips the BOM itself
await using (FileStream stream = File.OpenRead("stock-bom.json"))
{
    JsonElement root = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
    Console.WriteLine($"Stream: stock={root.GetProperty("stock")}");
}

// raw bytes: the BOM reaches the parser
try
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes("stock-bom.json"));
}
catch (JsonException ex)
{
    Console.WriteLine($"ReadAllBytes: {ex.Message}");
}
