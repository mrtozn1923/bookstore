#:package YamlDotNet@18.1.0
#:property PublishAot=false
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

string text = File.ReadAllText(args[0]);

// untyped: every scalar stays a string
object? untyped = new DeserializerBuilder().Build().Deserialize<object>(text);
Console.WriteLine($"untyped: {JsonSerializer.Serialize(untyped)}");

// typed: the target property's type decides
IDeserializer deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
Settings settings = deserializer.Deserialize<Settings>(text);
Console.WriteLine($"typed:   {JsonSerializer.Serialize(settings.Shipping)}");

class Settings
{
    public ShippingSettings Shipping { get; set; } = new();
}

class ShippingSettings
{
    public List<string> Countries { get; set; } = [];
    public string Cutoff { get; set; } = "";
    public bool Express { get; set; }
    public bool GiftWrap { get; set; }
    public int LabelFileMode { get; set; }
    public double MaxWeightGrams { get; set; }
    public string ApiVersion { get; set; } = "";
    public string? Note { get; set; }
}
