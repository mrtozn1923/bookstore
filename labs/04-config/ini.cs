#:package Microsoft.Extensions.Configuration.Ini@10.0.15
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddIniFile(args[0])
    .Build();

foreach ((string key, string? value) in config.AsEnumerable().Where(p => p.Value is not null).OrderBy(p => p.Key))
{
    Console.WriteLine($"{key} = [{value}]");
}
