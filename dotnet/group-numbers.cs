using System.Text.RegularExpressions;

Regex re = new(@"(?<year>\d{4})-(\d{2})");
Match m = re.Match("2026-11");

// in .NET, named groups are numbered after the unnamed ones
Console.WriteLine($"1={m.Groups[1].Value} 2={m.Groups[2].Value} year={m.Groups["year"].Value}");
Console.WriteLine(string.Join(",", re.GetGroupNames()));
Console.WriteLine(re.Replace("2026-11", "$2/$1"));

// the same name in two alternatives
Regex either = new(@"(?<year>\d{4})-\d{2}|\d{2}/(?<year>\d{4})");
Console.WriteLine($"{either.Match("2026-11").Groups["year"].Value} {either.Match("11/2026").Groups["year"].Value}");
