// Monthly list-price estimate for Bookstore, from the public Azure Retail Prices API (no sign-in needed).
// Run: dotnet run tools/cloud/cost-estimate.cs -- prod   (or dev)
using System.Text.Json;

const double HoursPerMonth = 730; // the month the Azure pricing pages use
string environment = args.FirstOrDefault() ?? "prod";

// Label, OData filter, meter name, monthly quantity in the meter's unit
(string Label, string Filter, string Meter, double Quantity)[] items = environment == "dev"
    ?
    [
        ("App Service B1 Linux, 1 instance", AppService("Azure App Service Basic Plan - Linux", "B1"), "B1", HoursPerMonth),
        ("Azure SQL free offer", "", "", 0),
        ("Blob hot LRS, 5 GB", Blob, "Hot LRS Data Stored", 5),
        ("Log Analytics ingestion, 3 GB", Logs, "Analytics Logs Data Ingestion", 3),
        ("Internet egress, 20 GB", Egress, "Standard Data Transfer Out", 20),
    ]
    :
    [
        ("App Service P0v3 Linux, 2 instances", AppService("Azure App Service Premium v3 Plan - Linux", "P0v3"), "P0v3 App", 2 * HoursPerMonth),
        ("Azure SQL General Purpose, 2 vCores", Sql, "vCore", HoursPerMonth),
        ("Blob hot LRS, 50 GB", Blob, "Hot LRS Data Stored", 50),
        ("Private endpoints, 2", PrivateEndpoint, "Standard Private Endpoint", 2 * HoursPerMonth),
        ("NAT gateway, 1", Nat, "Standard Gateway", HoursPerMonth),
        ("Log Analytics ingestion, 30 GB", Logs, "Analytics Logs Data Ingestion", 30),
        ("Internet egress, 300 GB", Egress, "Standard Data Transfer Out", 300),
    ];

using HttpClient http = new() { BaseAddress = new Uri("https://prices.azure.com/") };
double total = 0;
Console.WriteLine($"Bookstore {environment}, westeurope, USD list prices retrieved {DateTime.UtcNow:yyyy-MM-dd}");
foreach ((string label, string filter, string meter, double quantity) in items)
{
    double cost = filter.Length == 0 ? 0 : TieredCost(await GetTiersAsync(filter, meter), quantity);
    total += cost;
    Console.WriteLine($"  {label,-38} {cost,9:0.00}");
}
Console.WriteLine($"  {"Total per month",-38} {total,9:0.00}");

async Task<List<(double From, double Price)>> GetTiersAsync(string filter, string meter)
{
    string url = "api/retail/prices?$filter=" + Uri.EscapeDataString($"{filter} and meterName eq '{meter}' and priceType eq 'Consumption'");
    using JsonDocument doc = JsonDocument.Parse(await http.GetStringAsync(url));
    return doc.RootElement.GetProperty("Items").EnumerateArray()
        .Select(i => (From: i.GetProperty("tierMinimumUnits").GetDouble(), Price: i.GetProperty("retailPrice").GetDouble()))
        .Distinct()
        .OrderBy(t => t.From)
        .ToList();
}

// Each tier's price applies from its minimum to the next tier's minimum (e.g. the first 100 GB of egress are free)
static double TieredCost(List<(double From, double Price)> tiers, double quantity) =>
    tiers.Select((tier, i) =>
    {
        double upTo = i + 1 < tiers.Count ? tiers[i + 1].From : double.MaxValue;
        return Math.Max(0, Math.Min(quantity, upTo) - tier.From) * tier.Price;
    }).Sum();

static string AppService(string product, string sku) =>
    $"serviceName eq 'Azure App Service' and armRegionName eq 'westeurope' and productName eq '{product}' and skuName eq '{sku}'";

partial class Program
{
    const string Sql = "serviceName eq 'SQL Database' and armRegionName eq 'westeurope' and productName eq 'SQL Database Single/Elastic Pool General Purpose - Compute Gen5' and skuName eq '2 vCore'";
    const string Blob = "serviceName eq 'Storage' and armRegionName eq 'westeurope' and productName eq 'General Block Blob v2' and skuName eq 'Hot LRS'";
    // Private Link and NAT Gateway are priced in the "Global" region
    const string PrivateEndpoint = "serviceName eq 'Virtual Network' and armRegionName eq 'Global' and productName eq 'Virtual Network Private Link'";
    const string Nat = "serviceName eq 'NAT Gateway' and armRegionName eq 'Global' and skuName eq 'Standard'";
    const string Logs = "serviceName eq 'Log Analytics' and armRegionName eq 'westeurope' and skuName eq 'Analytics Logs'";
    const string Egress = "serviceName eq 'Bandwidth' and armRegionName eq 'westeurope' and productName eq 'Rtn Preference: MGN' and skuName eq 'Standard'";
}
