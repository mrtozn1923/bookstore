namespace Algorithms.Strings;

public static class NaiveSearch
{
    // Try every start position; on a mismatch, slide by one and start the pattern over
    public static int IndexOf(string text, string pattern, out long comparisons)
    {
        comparisons = 0;
        for (int start = 0; start <= text.Length - pattern.Length; start++)
        {
            int j = 0;
            while (j < pattern.Length)
            {
                comparisons++;
                if (text[start + j] != pattern[j]) break;
                j++;
            }

            if (j == pattern.Length) return start;
        }

        return -1;
    }
}
