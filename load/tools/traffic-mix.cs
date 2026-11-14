#:property PublishAot=false
// Reads W3C access logs and prints the request mix per endpoint and the share of the most viewed books.
// Usage: dotnet run load/tools/traffic-mix.cs -- <log folder>
using System.Globalization;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

string folder = args.Length > 0 ? args[0] : "logs";
Dictionary<string, int> endpoints = [];
Dictionary<string, int> bookViews = [];
int total = 0;
string[] fields = [];

foreach (string file in Directory.GetFiles(folder, "*.txt").Order())
{
    foreach (string line in File.ReadLines(file))
    {
        if (line.StartsWith("#Fields:"))
        {
            fields = line["#Fields:".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            continue;
        }
        if (line.StartsWith('#') || line.Length == 0)
        {
            continue;
        }
        string[] values = line.Split(' ');
        string method = values[Array.IndexOf(fields, "cs-method")];
        string path = values[Array.IndexOf(fields, "cs-uri-stem")];
        string query = values[Array.IndexOf(fields, "cs-uri-query")];

        string endpoint = method + " " + Normalize(path);
        if (endpoint == "GET /api/books" && query.Contains("search="))
        {
            endpoint = "GET /api/books?search=";
        }
        endpoints[endpoint] = endpoints.GetValueOrDefault(endpoint) + 1;
        if (endpoint == "GET /api/books/{isbn}")
        {
            bookViews[path] = bookViews.GetValueOrDefault(path) + 1;
        }
        total++;
    }
}

Console.WriteLine($"{total} requests");
foreach ((string endpoint, int count) in endpoints.OrderByDescending(e => e.Value))
{
    Console.WriteLine($"{count * 100.0 / total,6:F1}%  {endpoint}");
}

int views = bookViews.Values.Sum();
List<int> sorted = [.. bookViews.Values.OrderDescending()];
Console.WriteLine($"{bookViews.Count} distinct books viewed");
foreach (int top in new[] { 10, 100, 1000 })
{
    Console.WriteLine($"top {top,4} books: {sorted.Take(top).Sum() * 100.0 / views:F1}% of book views");
}

// /api/books/9780441172719 → /api/books/{isbn}, /api/carts/0196.../items/... → /api/carts/{id}/items/{isbn}
static string Normalize(string path)
{
    path = Regex.Replace(path, "[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}", "{id}");
    path = Regex.Replace(path, @"/\d{13}(?=/|$)", "/{isbn}");
    path = Regex.Replace(path, @"/orders/\d+$", "/orders/{id}");
    return path.TrimEnd('/');
}
