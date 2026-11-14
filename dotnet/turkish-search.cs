using System.Globalization;
using System.Text.RegularExpressions;

string[] catalog = ["ÇALIKUŞU", "İNCE MEMED", "ROBINSON CRUSOE: ISSIZ BİR ADADA 28 YIL"];
string[] terms = ["çalıkuşu", "ince memed", "ıssız"];

// culture name from the command line: tr-TR, en-US or "" (invariant)
if (args.Length > 0)
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(args[0]);
Console.WriteLine($"CurrentCulture: \"{CultureInfo.CurrentCulture.Name}\"");

foreach (string term in terms)
{
    Regex re = new(Regex.Escape(term), RegexOptions.IgnoreCase);
    Console.WriteLine($"  {term,-11} regex: {catalog.Count(re.IsMatch)}  tr-TR CompareInfo: {catalog.Count(t => ContainsTr(t, term))}");
}

// culture-aware search without regex: the culture is explicit, not inherited
static bool ContainsTr(string text, string term) =>
    CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(text, term, CompareOptions.IgnoreCase) >= 0;
