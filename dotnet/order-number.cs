using System.Text.RegularExpressions;

string[] inputs =
[
    "BKS-2026-000123",
    "BKS-2026-000123\n",
    "BKS-2026-٠٠٠١٢٣",
];

string[] patterns = [@"^BKS-\d{4}-\d{6}$", @"^BKS-[0-9]{4}-[0-9]{6}\z"];

foreach (string pattern in patterns)
{
    Console.WriteLine(pattern);
    foreach (string input in inputs)
    {
        bool ok = Regex.IsMatch(input, pattern);
        Console.WriteLine($"  {Show(input),-26} {ok}");
    }
}

// show \n visibly
static string Show(string s) => "\"" + s.Replace("\n", "\\n") + "\"";
