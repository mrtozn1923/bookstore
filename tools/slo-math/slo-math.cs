// Error budget arithmetic for an SLO: how much downtime and how many failed requests a target allows.
// Usage: dotnet run tools/slo-math/slo-math.cs -- <windowDays> <requestsPerDay> <target%>...
// Example: dotnet run tools/slo-math/slo-math.cs -- 30 5500 99 99.5 99.9 99.95 99.99
using System.Globalization;

CultureInfo inv = CultureInfo.InvariantCulture;
int windowDays = int.Parse(args[0], inv);
long requestsPerDay = long.Parse(args[1], inv);
decimal[] targets = [.. args.Skip(2).Select(a => decimal.Parse(a, inv))];

double windowMinutes = windowDays * 24 * 60;
long requestsInWindow = requestsPerDay * windowDays;
Console.WriteLine($"window: {windowDays} days = {windowMinutes:N0} minutes, {requestsInWindow:N0} requests");
Console.WriteLine($"{"target",-8} {"budget",8} {"downtime",12} {"per day",10} {"failed requests",16} {"per day",8}");
foreach (decimal target in targets)
{
    double budget = (double)(100m - target) / 100;
    double downMinutes = windowMinutes * budget;
    double failed = requestsInWindow * budget;
    Console.WriteLine(
        $"{target.ToString(inv) + "%",-8} {budget,8:P2} {FormatMinutes(downMinutes),12} {FormatMinutes(downMinutes / windowDays),10} " +
        $"{failed,16:N0} {failed / windowDays,8:N1}");
}

static string FormatMinutes(double minutes) =>
    minutes >= 60 ? $"{minutes / 60:0.0} h" : minutes >= 1 ? $"{minutes:0.0} min" : $"{minutes * 60:0} s";
