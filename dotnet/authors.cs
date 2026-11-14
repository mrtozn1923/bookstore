using System.Text.RegularExpressions;

string[] lines = File.ReadAllLines("data/authors.csv");

// first attempt: any quoted "Last, First" pair
Regex anyPair = new(@"""([^"",]+), ([^""]+)""");
// only the last column: the quoted field must end the line
Regex author = new(@"""(?<last>[^"",]+), (?<first>[^""]+)""$");

foreach (string line in lines)
    Console.WriteLine(anyPair.Replace(line, "\"$2 $1\""));
Console.WriteLine();

foreach (string line in lines)
    Console.WriteLine(author.Replace(line, "\"${first} ${last}\""));
Console.WriteLine();

// a MatchEvaluator when the new text needs code
foreach (string line in lines)
    Console.WriteLine(author.Replace(line, m => $"\"{m.Groups["last"].Value.ToUpperInvariant()}, {m.Groups["first"].Value}\""));
