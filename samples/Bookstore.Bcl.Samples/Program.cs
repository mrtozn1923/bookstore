using System.Globalization;
using Bookstore.Bcl.Samples.Formatting;
using Bookstore.Bcl.Samples.Strings;

// Usage: dotnet run --project samples/Bookstore.Bcl.Samples -- <demo> [--culture tr-TR|en-US|invariant]
var demos = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
foreach (var (name, demo) in StringDemos.All.Concat(FormatDemos.All)) demos[name] = demo;

int cultureIndex = Array.IndexOf(args, "--culture");
if (cultureIndex >= 0 && cultureIndex + 1 < args.Length)
{
    string cultureName = args[cultureIndex + 1];
    CultureInfo culture = cultureName.Equals("invariant", StringComparison.OrdinalIgnoreCase)
        ? CultureInfo.InvariantCulture
        : CultureInfo.GetCultureInfo(cultureName);
    CultureInfo.CurrentCulture = culture;
    CultureInfo.CurrentUICulture = culture;
}

if (args.Length == 0 || !demos.TryGetValue(args[0], out Action? run))
{
    Console.Error.WriteLine("Demos: " + string.Join(", ", demos.Keys.Order(StringComparer.Ordinal)));
    return 1;
}

run();
return 0;
