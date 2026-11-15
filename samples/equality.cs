IsbnClass a = new("9780441013593");
IsbnClass b = new("9780441013593");
IsbnRecord c = new("9780441013593");
IsbnRecord d = new("9780441013593");

Console.WriteLine($"class : a == b -> {a == b}, Equals -> {a.Equals(b)}");
Console.WriteLine($"record: c == d -> {c == d}, Equals -> {d.Equals(c)}");

HashSet<IsbnClass> seen = [a];
Console.WriteLine($"class in HashSet: {seen.Contains(b)}");

public sealed class IsbnClass(string value)
{
    public string Value { get; } = value;
}

public sealed record IsbnRecord(string Value);
