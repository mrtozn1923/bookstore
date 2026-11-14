using System.Text.RegularExpressions;

// the same probe file in .NET: first matched text, "-" or the end of the exception message
Console.WriteLine($"{"probe",-14}.NET");
foreach (string line in File.ReadLines("data/flavor-probes.tsv"))
{
    string[] parts = line.Split('\t');
    string result;
    try
    {
        Match m = Regex.Match(parts[2], parts[1]);
        result = m.Success ? m.Value : "-";
    }
    catch (ArgumentException ex)
    {
        result = "error: " + ex.Message.Split(". ")[^1].TrimEnd('.');
    }
    Console.WriteLine($"{parts[0],-14}{result}");
}
