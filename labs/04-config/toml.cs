#:package Tomlyn@2.11.1
#:property PublishAot=false
using Tomlyn;

string text = File.ReadAllText("settings/shipping.toml");
Settings settings = TomlSerializer.Deserialize<Settings>(text)!;
ShippingSettings shipping = settings.Shipping;

// TOML has real date/time types: Cutoff arrives as TimeOnly, not as a string
Console.WriteLine($"Cutoff: {shipping.Cutoff:HH:mm} ({shipping.Cutoff.GetType().Name})");
Console.WriteLine($"MaxWeightGrams: {shipping.MaxWeightGrams}, ApiVersion: {shipping.ApiVersion}");
foreach (Rate rate in shipping.Rates)
{
    Console.WriteLine($"Rate: {rate.Region} {rate.Price}");
}

class Settings
{
    public ShippingSettings Shipping { get; set; } = new();
}

class ShippingSettings
{
    public string DefaultCountry { get; set; } = "";
    public TimeOnly Cutoff { get; set; }
    public bool Express { get; set; }
    public int MaxWeightGrams { get; set; }
    public string ApiVersion { get; set; } = "";
    public List<string> Carriers { get; set; } = [];
    public List<Rate> Rates { get; set; } = [];
}

class Rate
{
    public string Region { get; set; } = "";
    public decimal Price { get; set; }
}
