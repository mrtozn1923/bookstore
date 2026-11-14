using System.Text.RegularExpressions;

string[] titles = File.ReadAllLines("data/titles.txt");

foreach (string pattern in new[] { @"\p{IsCyrillic}", @"^\p{Lu}\p{Ll}+$", @"\p{Script=Cyrillic}", @"\p{Cyrillic}" })
{
    try
    {
        Console.WriteLine($"{pattern,-20} {string.Join(" | ", titles.Where(t => Regex.IsMatch(t, pattern)))}");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"{pattern,-20} {ex.Message}");
    }
}
