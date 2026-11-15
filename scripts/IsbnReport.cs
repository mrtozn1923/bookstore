// Shared by the scripts in this folder through #:include.
sealed class IsbnReport
{
    private readonly List<string> invalid = [];

    public int InvalidCount => invalid.Count;

    public void Add(string input, bool isValid)
    {
        if (!isValid)
        {
            invalid.Add(input);
        }
    }

    public void Print()
    {
        foreach (string input in invalid)
        {
            Console.WriteLine($"invalid: {input}");
        }

        Console.WriteLine($"{invalid.Count} invalid ISBN(s)");
    }
}
