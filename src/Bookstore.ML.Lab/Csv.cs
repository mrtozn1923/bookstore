using System.Text;

namespace Bookstore.ML.Lab;

/// <summary>Minimal RFC 4180 reader: commas, quoted fields and doubled quotes. Enough for the generated files.</summary>
public static class Csv
{
    public static IEnumerable<string[]> Read(string path, bool skipHeader = true)
    {
        using StreamReader reader = new(path);
        if (skipHeader) reader.ReadLine();
        while (reader.ReadLine() is string line)
        {
            yield return Split(line);
        }
    }

    public static string[] Split(string line)
    {
        List<string> fields = [];
        StringBuilder current = new();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else current.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        fields.Add(current.ToString());
        return [.. fields];
    }
}
