using System.Buffers;
using System.Globalization;
using Algorithms.Strings;

namespace Algorithms.Demos.Demos;

public static class StringDemos
{
    public static void WorstCase()
    {
        string text = new('a', 10_000);
        string pattern = new string('a', 99) + "b";
        NaiveSearch.IndexOf(text, pattern, out long naive);
        Kmp.FindAll(text, pattern, out long kmp);
        Console.WriteLine($"text n={text.Length}, pattern m={pattern.Length}");
        Console.WriteLine($"naive comparisons: {naive:N0}");
        Console.WriteLine($"KMP comparisons:   {kmp:N0}");
        Console.WriteLine($"failure(\"ababaca\") = [{string.Join(", ", Kmp.BuildFailure("ababaca"))}]");
    }

    public static void Moderation()
    {
        string[] banned = ["scam", "casino", "free money", "click here", "spam"];
        AhoCorasick ac = new(banned);
        string review = "Great book! Not a scam. Click here for free money at our casino.";
        foreach ((int index, string word) in ac.FindAll(review.ToLowerInvariant()))
            Console.WriteLine($"{index,3}: {word}");

        SearchValues<string> sv = SearchValues.Create(banned, StringComparison.OrdinalIgnoreCase);
        ReadOnlySpan<char> span = review;
        Console.WriteLine($"ContainsAny: {span.ContainsAny(sv)}, first at {span.IndexOfAny(sv)}");
        AhoCorasick classic = new(["he", "she", "his", "hers"]);
        Console.WriteLine("ushers -> " + string.Join(", ", classic.FindAll("ushers").Select(m => $"{m.Pattern}@{m.Index}")));
        Console.WriteLine($"trie nodes for the 5 banned phrases: {ac.NodeCount}");
    }

    public static void Authors()
    {
        string[] authors = ["Jane Austen", "Mary Shelley", "George Eliot", "Frank Herbert", "J. R. R. Tolkien", "Bram Stoker", "Austen"];
        string description = "A study guide comparing Mary Shelley and Bram Stoker, with a chapter on how George Eliot read Jane Austen.";
        AhoCorasick ac = new(authors);
        foreach ((int index, string name) in ac.FindAll(description))
            Console.WriteLine($"{index,3}: {name}");
    }

    public static void Turkish()
    {
        string text = "KİTAP KULÜBÜ: Istanbul'da bu hafta";
        Console.WriteLine($"OrdinalIgnoreCase \"kitap\":    {text.Contains("kitap", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"tr-TR IgnoreCase \"kitap\":     {CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(text, "kitap", CompareOptions.IgnoreCase) >= 0}");
        Console.WriteLine($"OrdinalIgnoreCase \"istanbul\": {text.Contains("istanbul", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"tr-TR IgnoreCase \"istanbul\":  {CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(text, "istanbul", CompareOptions.IgnoreCase) >= 0}");
        SearchValues<string> sv = SearchValues.Create(["kitap"], StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"SearchValues OrdinalIgnoreCase: {text.AsSpan().ContainsAny(sv)}");
        string folded = text.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        Console.WriteLine($"tr-TR ToLower: \"{folded}\" contains kitap: {folded.Contains("kitap", StringComparison.Ordinal)}");
    }

    public static void Plagiarism()
    {
        string original = "Dune follows Paul Atreides as his family takes control of the desert planet Arrakis, the only source of the spice melange.";
        string submitted = "In this classic, Paul Atreides and his family takes control of the desert planet Arrakis, and war follows.";
        const int k = 20;
        HashSet<string> windows = [.. Enumerable.Range(0, original.Length - k + 1).Select(i => original.Substring(i, k))];
        List<int> starts = [.. RabinKarp.FindAll(submitted, windows).Select(m => m.Index).Distinct().Order()];
        Console.WriteLine($"{windows.Count} windows of {k} chars, {starts.Count} matching positions in the submission");
        // Merge consecutive start positions into copied ranges
        int from = starts[0], prev = starts[0];
        foreach (int s in starts.Skip(1).Append(int.MaxValue))
        {
            if (s == prev + 1) { prev = s; continue; }
            Console.WriteLine($"copied: \"{submitted.Substring(from, prev - from + k)}\"");
            from = prev = s;
        }
    }
}
