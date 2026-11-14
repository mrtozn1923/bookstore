// Bookstore naming and tagging convention: <abbreviation>-<workload>-<environment>-<region>
// Run: dotnet run tools/cloud/naming.cs -- [workload] [environment...]
using System.Text.RegularExpressions;

string workload = args.Length > 0 ? args[0] : "bookstore";
string[] environments = args.Length > 1 ? args[1..] : ["dev", "prod"];
const string Region = "westeurope";
const string RegionShort = "weu";

// Abbreviations follow the Cloud Adoption Framework list; limits follow "Naming rules and restrictions for Azure resources".
NamingRule[] rules =
[
    new("Resource group", "rg", 1, 90, @"^[\w\-\.\(\)]*[\w\-\(\)]$", Scope.Subscription),
    new("App Service plan", "asp", 1, 60, "^[a-zA-Z0-9-]+$", Scope.ResourceGroup),
    new("Web app", "app", 2, 60, "^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$", Scope.Global),
    new("Container Apps environment", "cae", 2, 32, "^[a-z][a-z0-9-]*[a-z0-9]$", Scope.ResourceGroup),
    new("Container app", "ca", 2, 32, "^[a-z][a-z0-9-]*[a-z0-9]$", Scope.ResourceGroup),
    new("Storage account", "st", 3, 24, "^[a-z0-9]+$", Scope.Global, Compact: true),
    new("SQL logical server", "sql", 1, 63, "^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", Scope.Global),
    new("Key vault", "kv", 3, 24, "^[a-zA-Z](?!.*--)[a-zA-Z0-9-]*[a-zA-Z0-9]$", Scope.Global),
    new("User-assigned identity", "id", 3, 128, "^[a-zA-Z0-9][a-zA-Z0-9_-]*$", Scope.ResourceGroup),
];

Dictionary<string, string> baseTags = new()
{
    ["app"] = workload,
    ["owner"] = "platform-team@example.com",
    ["cost-center"] = "cc-1042",
};

int errors = 0;
foreach (string environment in environments)
{
    Console.WriteLine($"== {environment} ({Region})");
    foreach (NamingRule rule in rules)
    {
        string name = rule.Compact
            ? $"{rule.Abbreviation}{workload}{environment}{RegionShort}".Replace("-", "").ToLowerInvariant()
            : $"{rule.Abbreviation}-{workload}-{environment}-{RegionShort}".ToLowerInvariant();
        string? problem = rule.Check(name);
        errors += problem is null ? 0 : 1;
        string scope = rule.Scope == Scope.Global ? "global" : rule.Scope.ToString().ToLowerInvariant();
        Console.WriteLine($"  {rule.Type,-27} {name,-40} {name.Length,3}/{rule.MaxLength,-3} {scope,-14} {problem ?? "OK"}");
    }
    Dictionary<string, string> tags = new(baseTags) { ["environment"] = environment };
    Console.WriteLine("  tags: " + string.Join(" ", tags.Select(t => $"{t.Key}={t.Value}")));
}
return errors == 0 ? 0 : 1;

enum Scope { Global, Subscription, ResourceGroup }

record NamingRule(string Type, string Abbreviation, int MinLength, int MaxLength, string Pattern, Scope Scope, bool Compact = false)
{
    public string? Check(string name) =>
        name.Length < MinLength || name.Length > MaxLength ? $"ERROR length must be {MinLength}-{MaxLength}"
        : !Regex.IsMatch(name, Pattern) ? "ERROR invalid characters"
        : null;
}
