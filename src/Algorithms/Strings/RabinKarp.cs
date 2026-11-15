namespace Algorithms.Strings;

public static class RabinKarp
{
    private const ulong Base = 131;
    private const ulong Mod = (1UL << 61) - 1; // a Mersenne prime keeps collisions rare

    // Finds every position where any of the same-length patterns occurs; hashes are verified with a real comparison
    public static List<(int Index, string Pattern)> FindAll(string text, IReadOnlyCollection<string> patterns)
    {
        List<(int, string)> matches = [];
        if (patterns.Count == 0) return matches;
        int m = patterns.First().Length;
        if (m == 0 || patterns.Any(p => p.Length != m)) throw new ArgumentException("Patterns must have the same non-zero length.");
        if (text.Length < m) return matches;

        Dictionary<ulong, List<string>> byHash = [];
        foreach (string p in patterns.Distinct())
        {
            ulong h = Hash(p.AsSpan());
            if (!byHash.TryGetValue(h, out List<string>? list)) byHash[h] = list = [];
            list.Add(p);
        }

        ulong highPower = 1; // Base^(m-1): weight of the character leaving the window
        for (int i = 1; i < m; i++) highPower = MulMod(highPower, Base);

        ulong window = Hash(text.AsSpan(0, m));
        for (int start = 0; ; start++)
        {
            if (byHash.TryGetValue(window, out List<string>? candidates))
            {
                foreach (string c in candidates)
                {
                    if (text.AsSpan(start, m).SequenceEqual(c)) matches.Add((start, c));
                }
            }

            if (start + m == text.Length) break;
            // Roll: remove text[start], shift, add text[start + m]
            window = AddMod(window, Mod - MulMod(text[start], highPower));
            window = AddMod(MulMod(window, Base), text[start + m]);
        }

        return matches;
    }

    public static ulong Hash(ReadOnlySpan<char> s)
    {
        ulong h = 0;
        foreach (char c in s) h = AddMod(MulMod(h, Base), c);
        return h;
    }

    private static ulong AddMod(ulong a, ulong b)
    {
        ulong r = a + b;
        return r >= Mod ? r - Mod : r;
    }

    private static ulong MulMod(ulong a, ulong b)
    {
        UInt128 product = (UInt128)a * b;
        ulong lo = (ulong)(product & Mod);
        ulong hi = (ulong)(product >> 61);
        return AddMod(lo, hi);
    }
}
