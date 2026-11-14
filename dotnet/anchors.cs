using System.Text.RegularExpressions;

string batch = "BKS-2026-000123\nBKS-2026-000124\nBKS-2026-000125\n";

// without Multiline, ^ and $ refer to the whole string
Console.WriteLine(Regex.Matches(batch, @"^BKS-\d+-\d+$").Count);
Console.WriteLine(Regex.Matches(batch, @"^BKS-\d+-\d+$", RegexOptions.Multiline).Count);

// $ also matches before a final \n; \z does not, \Z behaves like $
string input = "BKS-2026-000123\n";
Console.WriteLine($"{Regex.IsMatch(input, @"^BKS-\d+-\d+$")} {Regex.IsMatch(input, @"\ABKS-\d+-\d+\Z")} {Regex.IsMatch(input, @"\ABKS-\d+-\d+\z")}");

// \b uses .NET's Unicode \w, so "kitapçı" has no boundary after "kitap"
Console.WriteLine(Regex.IsMatch("kitapçı", @"\bkitap\b"));
