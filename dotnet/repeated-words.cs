using System.Text.RegularExpressions;

foreach (string line in File.ReadLines("data/descriptions.txt"))
{
    // \k<word> must repeat exactly what the group captured
    foreach (Match m in Regex.Matches(line, @"\b(?<word>\w+) \k<word>\b", RegexOptions.IgnoreCase))
        Console.WriteLine($"{line[..13]} \"{m.Value}\" at {m.Index}");
}

// alternation: the first alternative that matches wins
Console.WriteLine(Regex.Match("BKS-2026-000123", "BKS|BKS-2026").Value);

// ^ binds to "paperback" only, $ to "ebook" only
Console.WriteLine($"{Regex.IsMatch("paperback edition", "^paperback|ebook$")} {Regex.IsMatch("paperback edition", "^(?:paperback|ebook)$")}");
