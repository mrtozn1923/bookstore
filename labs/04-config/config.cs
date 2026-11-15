#:package Microsoft.Extensions.Configuration.Json@10.0.15
#:package Microsoft.Extensions.Configuration.Ini@10.0.15
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@10.0.15
#:package Microsoft.Extensions.Configuration.Binder@10.0.15
#:package NetEscapades.Configuration.Yaml@3.1.0
#:package YamlDotNet@18.1.0
#:property PublishAot=false
using Microsoft.Extensions.Configuration;

string format = args[0];
ConfigurationBuilder builder = new();
builder.SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "settings"));

// one provider per format; the keys they produce are the same
_ = format switch
{
    "json" => builder.AddJsonFile("shipping.json"),
    "ini" => builder.AddIniFile("shipping.ini"),
    "yaml" => builder.AddYamlFile("shipping.yaml"),
    "env" => builder.AddEnvironmentVariables(),
    _ => throw new ArgumentException($"unknown format {format}")
};
IConfigurationRoot config = builder.Build();

foreach ((string key, string? value) in config.GetSection("Shipping").AsEnumerable(makePathsRelative: false).OrderBy(p => p.Key))
{
    if (value is not null)
    {
        Console.WriteLine($"{key} = {value}");
    }
}

try
{
    ShippingOptions options = config.GetSection("Shipping").Get<ShippingOptions>()!;
    Console.WriteLine($"Bound: {options.DefaultCountry}, {options.Cutoff:HH:mm}, Express={options.Express}, {options.ApiVersion}, [{string.Join(", ", options.Carriers)}]");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Binding failed: {ex.Message}");
}

class ShippingOptions
{
    public string DefaultCountry { get; set; } = "";
    public TimeOnly Cutoff { get; set; }
    public bool Express { get; set; }
    public int MaxWeightGrams { get; set; }
    public string ApiVersion { get; set; } = "";
    public List<string> Carriers { get; set; } = [];
}
