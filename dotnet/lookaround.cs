using System.Text.RegularExpressions;

// lookahead: a position followed by groups of three digits up to the end of the number
Console.WriteLine(Regex.Replace("Total: 1234567 TRY, 950 items", @"(?<=\d)(?=(?:\d{3})+\b)", "."));

// lookbehind: the amount after "price:", without "price:" in the match
Console.WriteLine(Regex.Match("price: 450 TRY", @"(?<=price:\s*)\d+").Value);

// negative lookahead: Payments lines that are not debug lines
string log = File.ReadAllText("data/app.log");
foreach (Match m in Regex.Matches(log, @"^(?!.* DBG ).*Bookstore\.Payments.*$", RegexOptions.Multiline))
    Console.WriteLine(m.Value.TrimEnd('\r'));

// a composition rule built from lookaheads
Regex rule = new(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^\w\s]).{8,}$");
foreach (string candidate in new[] { "Password1!", "Bookstore2026!", "correct horse battery staple" })
    Console.WriteLine($"{candidate,-30} {rule.IsMatch(candidate)}");
