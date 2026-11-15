using System.Globalization;
using Bookstore.Bcl.Samples.Encodings;
using Bookstore.Bcl.Samples.Formatting;
using Bookstore.Bcl.Samples.Globalization;
using Bookstore.Bcl.Samples.Numbers;
using Bookstore.Bcl.Samples.Strings;

// Usage: dotnet run --project samples/Bookstore.Bcl.Samples -- <demo> [--culture tr-TR|en-US|invariant]
var demos = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase);
foreach (var (name, demo) in StringDemos.All.Concat(FormatDemos.All).Concat(NumberDemos.All).Concat(EncodingDemos.All))
    demos[name] = () =>
    {
        demo();
        return Task.CompletedTask;
    };
foreach (var (name, demo) in GlobalizationDemos.All)
    demos[name] = demo;

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

if (args.Length == 0 || !demos.TryGetValue(args[0], out Func<Task>? run))
{
    Console.Error.WriteLine("Demos: " + string.Join(", ", demos.Keys.Order(StringComparer.Ordinal)));
    return 1;
}

await run();
return 0;
