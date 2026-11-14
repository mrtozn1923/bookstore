// Composite SLA and downtime budget for Bookstore's request path.
// Run: dotnet run tools/cloud/sla.cs

const double MinutesPerMonth = 30 * 24 * 60; // 43,200 minutes, the 30-day month the SLA tables use

(string Name, double Sla)[] chain =
[
    ("App Service (Bookstore.Api)", 99.95),
    ("Azure SQL Database (BookstoreDb)", 99.99),
];

double composite = chain.Aggregate(1.0, (product, part) => product * part.Sla / 100) * 100;

Console.WriteLine("Serial dependencies (all must be up):");
foreach ((string name, double sla) in chain)
{
    Console.WriteLine($"  {name,-34} {sla,7:0.###}%  {Downtime(sla),6:0.0} min/month");
}
Console.WriteLine($"  {"Composite",-34} {composite,7:0.###}%  {Downtime(composite),6:0.0} min/month");

Console.WriteLine();
Console.WriteLine("Downtime budget per 30-day month:");
foreach (double target in new[] { 99.0, 99.9, 99.95, 99.99, 99.999 })
{
    Console.WriteLine($"  {target,7:0.###}%  {Downtime(target),7:0.0} min");
}

// Two regions behind a failover router: the app is down only if both regions are down.
double twoRegions = (1 - Math.Pow(1 - composite / 100, 2)) * 100;
Console.WriteLine();
Console.WriteLine($"Same stack in two regions (ignoring the router): {twoRegions:0.#####}%  {Downtime(twoRegions):0.00} min/month");

static double Downtime(double slaPercent) => (1 - slaPercent / 100) * MinutesPerMonth;
