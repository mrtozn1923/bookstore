using System.Text.Json;
using System.Text.RegularExpressions;

string json = """{"orderId":"BKS-2026-000123","shipment":{"status":"Delayed"},"status":"Paid"}""";

// the regex finds the first "status" key, whichever object it belongs to
Console.WriteLine(Regex.Match(json, @"""status"":""([^""]*)""").Groups[1].Value);

// the parser knows which "status" belongs to the order
Console.WriteLine(JsonDocument.Parse(json).RootElement.GetProperty("status").GetString());
