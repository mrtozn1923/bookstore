using System.Globalization;
using System.Text.RegularExpressions;

string title = "ROBINSON CRUSOE: ISSIZ BİR ADADA 28 YIL";

// new Regex: the culture is read when the Regex is constructed, not when it matches
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
Regex builtUnderTurkish = new("ıssız", RegexOptions.IgnoreCase);
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Regex builtUnderInvariant = new("ıssız", RegexOptions.IgnoreCase);
Console.WriteLine($"new Regex, built under tr-TR:     {builtUnderTurkish.IsMatch(title)}");
Console.WriteLine($"new Regex, built under invariant: {builtUnderInvariant.IsMatch(title)}");

// GeneratedRegex: the culture is fixed at compile time by cultureName
Console.WriteLine($"generated, cultureName tr-TR:     {Titles.IssizTurkish.IsMatch(title)}");
Console.WriteLine($"generated, no cultureName:        {Titles.InceDefault.IsMatch("İNCE MEMED")}");

// the same pattern and options a second time, now with tr-TR
Console.WriteLine($"generated, same pattern, tr-TR:   {Titles.InceTurkish.IsMatch("İNCE MEMED")}");
Console.WriteLine($"same instance: {ReferenceEquals(Titles.InceDefault, Titles.InceTurkish)}");

static partial class Titles
{
    [GeneratedRegex("ıssız", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex IssizTurkish { get; }

    [GeneratedRegex("ince", RegexOptions.IgnoreCase)]
    public static partial Regex InceDefault { get; }

    [GeneratedRegex("ince", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex InceTurkish { get; }
}
