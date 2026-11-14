#:property PublishAot=false
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

// Checks NuGet package ids before they reach the build: does the package exist, how widely is it used,
// is its id prefix reserved by a verified owner? Ids come from Directory.Packages.props, or from the arguments.
const long MinDownloads = 100_000;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string[] ids = args.Length > 0
    ? args
    : [.. XDocument.Load("Directory.Packages.props").Descendants("PackageVersion")
        .Select(e => (string?)e.Attribute("Include")).OfType<string>()];

using HttpClient http = new();
ServiceIndex index = (await http.GetFromJsonAsync<ServiceIndex>("https://api.nuget.org/v3/index.json"))!;
string search = index.Resources.First(r => r.Type.StartsWith("SearchQueryService")).Id;

int problems = 0;
foreach (string id in ids)
{
    SearchResult result = (await http.GetFromJsonAsync<SearchResult>(
        $"{search}?q=packageid:{Uri.EscapeDataString(id)}&prerelease=true&semVerLevel=2.0.0"))!;
    Package? package = result.Data.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    string verdict = package switch
    {
        null => "NOT FOUND",
        { TotalDownloads: < MinDownloads } => "LOW USE",
        _ => "ok",
    };
    if (verdict != "ok")
    {
        problems++;
    }
    Console.WriteLine(package is null
        ? $"{verdict,-10} {id}"
        : $"{verdict,-10} {package.Id,-45} latest={package.Version,-8} downloads={package.TotalDownloads,13:N0} verified={package.Verified}");
}

Console.WriteLine(problems == 0 ? "All packages look established." : $"{problems} package(s) need a human decision.");
return problems == 0 ? 0 : 1;

record ServiceIndex(List<Resource> Resources);
record Resource([property: JsonPropertyName("@id")] string Id, [property: JsonPropertyName("@type")] string Type);
record SearchResult(List<Package> Data);
record Package(string Id, string Version, long TotalDownloads, bool Verified);
