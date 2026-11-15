namespace Algorithms.Strings;

public static class Kmp
{
    // failure[i] = length of the longest proper prefix of pattern[..(i + 1)] that is also its suffix
    public static int[] BuildFailure(string pattern)
    {
        int[] failure = new int[pattern.Length];
        int k = 0;
        for (int i = 1; i < pattern.Length; i++)
        {
            while (k > 0 && pattern[i] != pattern[k]) k = failure[k - 1];
            if (pattern[i] == pattern[k]) k++;
            failure[i] = k;
        }

        return failure;
    }

    public static List<int> FindAll(string text, string pattern) => FindAll(text, pattern, out _);

    public static List<int> FindAll(string text, string pattern, out long comparisons)
    {
        comparisons = 0;
        List<int> matches = [];
        if (pattern.Length == 0) return matches;
        int[] failure = BuildFailure(pattern);
        int k = 0; // how many pattern characters currently match
        for (int i = 0; i < text.Length; i++)
        {
            while (k > 0 && text[i] != pattern[k])
            {
                comparisons++;
                k = failure[k - 1]; // fall back inside the pattern; i never moves back
            }

            comparisons++;
            if (text[i] == pattern[k]) k++;
            if (k == pattern.Length)
            {
                matches.Add(i - k + 1);
                k = failure[k - 1];
            }
        }

        return matches;
    }
}
