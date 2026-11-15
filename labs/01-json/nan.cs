#:property PublishAot=false
using System.Text.Json;
using System.Text.Json.Serialization;

try
{
    Console.WriteLine(JsonSerializer.Serialize(new { AvgRating = double.NaN }));
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}

JsonSerializerOptions named = new() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
Console.WriteLine(JsonSerializer.Serialize(new { AvgRating = double.NaN }, named));
