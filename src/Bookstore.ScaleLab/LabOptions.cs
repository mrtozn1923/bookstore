namespace Bookstore.ScaleLab;

public sealed record LabOptions(IReadOnlyDictionary<string, string> Values)
{
    public static LabOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            values[args[i].TrimStart('-')] = args[i + 1];
        }
        return new LabOptions(values);
    }

    public string Get(string name, string fallback) => Values.TryGetValue(name, out string? v) ? v : fallback;

    public int GetInt(string name, int fallback) => Values.TryGetValue(name, out string? v) ? int.Parse(v) : fallback;
}
