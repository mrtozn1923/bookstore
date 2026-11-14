using NuGet.Versioning;

string[] input =
[
    "1.0.0", "1.0.0-rc.1", "1.0.0-beta.11", "1.0.0-beta.2",
    "1.0.0-alpha.beta", "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-beta",
    "0.9.12"
];

var sorted = input.Select(NuGetVersion.Parse).Order(VersionComparer.VersionRelease);
Console.WriteLine(string.Join(" < ", sorted.Select(v => v.ToFullString())));

var a = NuGetVersion.Parse("1.0.0+sha.5114f85");
var b = NuGetVersion.Parse("1.0.0+sha.0c2d9e1");
Console.WriteLine($"build metadata ignored: {a == b}");
Console.WriteLine($"with metadata compared: {VersionComparer.VersionReleaseMetadata.Equals(a, b)}");

var upper = NuGetVersion.Parse("1.0.0-RC.1");
var lower = NuGetVersion.Parse("1.0.0-rc.1");
Console.WriteLine($"RC.1 vs rc.1 (NuGet): {upper.CompareTo(lower)}");

foreach (var text in new[] { "1.0.0-beta.01", "1.0", "2026.04.1", "v1.2.3" })
{
    Console.WriteLine($"{text,-14} SemVer: {SemanticVersion.TryParse(text, out _),-5} NuGet: {(NuGetVersion.TryParse(text, out var v) ? v.ToNormalizedString() : "invalid")}");
}
