using Algorithms.Strings;

namespace Algorithms.Tests.Strings;

public class StringSearchTests
{
    [Fact]
    public void BuildFailure_ClassicPattern_ReturnsPrefixFunction()
    {
        Assert.Equal([0, 0, 1, 2, 3, 0, 1], Kmp.BuildFailure("ababaca"));
    }

    [Fact]
    public void FindAll_OverlappingMatches_ReturnsEveryStart()
    {
        Assert.Equal([0, 1, 2], Kmp.FindAll("aaaa", "aa"));
    }

    [Fact]
    public void AhoCorasick_Ushers_FindsOverlappingPatterns()
    {
        AhoCorasick ac = new(["he", "she", "his", "hers"]);
        List<(int, string)> found = [.. ac.FindAll("ushers")];

        Assert.Equal([(1, "she"), (2, "he"), (2, "hers")], found);
    }

    [Fact]
    public void AllAlgorithms_RandomTexts_MatchBruteForce()
    {
        Random random = new(24);
        for (int round = 0; round < 300; round++)
        {
            string text = RandomString(random, random.Next(0, 60));
            string[] patterns = [.. Enumerable.Range(0, 5).Select(_ => RandomString(random, 3)).Distinct()];
            foreach (string p in patterns)
            {
                List<int> expected = BruteForce(text, p);
                Assert.Equal(expected, Kmp.FindAll(text, p));
                Assert.Equal(expected.Count > 0 ? expected[0] : -1, NaiveSearch.IndexOf(text, p, out _));
                Assert.Equal(expected.Count > 0 ? expected[0] : -1, text.IndexOf(p, StringComparison.Ordinal));
            }

            List<(int, string)> all = [.. patterns.SelectMany(p => BruteForce(text, p).Select(i => (i, p))).OrderBy(x => x.i).ThenBy(x => x.p, StringComparer.Ordinal)];
            Assert.Equal(all, RabinKarp.FindAll(text, patterns).OrderBy(x => x.Index).ThenBy(x => x.Pattern, StringComparer.Ordinal));
            Assert.Equal(all, new AhoCorasick(patterns).FindAll(text).OrderBy(x => x.Index).ThenBy(x => x.Pattern, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void ContainsAny_SameAnswerAsSearchValues()
    {
        string[] banned = ["scam", "casino", "free money"];
        AhoCorasick ac = new(banned);
        System.Buffers.SearchValues<string> sv = System.Buffers.SearchValues.Create(banned, StringComparison.Ordinal);
        foreach (string review in new[] { "Great book, loved it.", "Click for free money", "casino night", "" })
        {
            Assert.Equal(review.AsSpan().ContainsAny(sv), ac.ContainsAny(review));
        }
    }

    private static string RandomString(Random random, int length) =>
        new([.. Enumerable.Range(0, length).Select(_ => (char)('a' + random.Next(2)))]);

    private static List<int> BruteForce(string text, string p) =>
        [.. Enumerable.Range(0, Math.Max(0, text.Length - p.Length + 1)).Where(i => string.CompareOrdinal(text, i, p, 0, p.Length) == 0)];
}
