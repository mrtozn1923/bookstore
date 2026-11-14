using System.Globalization;
using System.Text;

namespace Bookstore.SearchLab;

// A toy inverted index: the same idea Lucene uses, without compression or segments.
public static class InvertedIndexDemo
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    // Document ids start at 1, like the _id values we index into Elasticsearch.
    private static readonly Dictionary<int, string> Titles = new()
    {
        [1] = "İnce Memed",
        [2] = "İnce Memed 2",
        [3] = "Kürk Mantolu Madonna"
    };

    public static void Run(string[] args)
    {
        string query = args.Length > 0 ? string.Join(' ', args) : "memed";

        // term -> (document id -> term frequency)
        SortedDictionary<string, SortedDictionary<int, int>> postings = new(StringComparer.Ordinal);
        Dictionary<int, int> lengths = [];

        foreach ((int docId, string title) in Titles)
        {
            List<string> terms = Tokenize(title);
            lengths[docId] = terms.Count;
            foreach (string term in terms)
            {
                if (!postings.TryGetValue(term, out SortedDictionary<int, int>? docs))
                {
                    docs = [];
                    postings[term] = docs;
                }
                docs[docId] = docs.GetValueOrDefault(docId) + 1;
            }
        }

        Console.WriteLine("Documents:");
        foreach ((int docId, string title) in Titles)
        {
            Console.WriteLine($"  {docId}: {title} (length {lengths[docId]})");
        }

        Console.WriteLine("Inverted index (term -> posting list):");
        foreach ((string term, SortedDictionary<int, int> docs) in postings)
        {
            Console.WriteLine($"  {term,-8} -> [{string.Join(", ", docs.Keys)}]");
        }

        // BM25 as Lucene computes it: idf * tf part, with k1 = 1.2 and b = 0.75.
        const double k1 = 1.2;
        const double b = 0.75;
        int n = Titles.Count;
        double avgLength = lengths.Values.Average();
        Dictionary<int, double> scores = [];

        foreach (string term in Tokenize(query))
        {
            if (!postings.TryGetValue(term, out SortedDictionary<int, int>? docs))
            {
                Console.WriteLine($"Query term '{term}': no posting list, contributes nothing");
                continue;
            }

            double idf = Math.Log(1 + (n - docs.Count + 0.5) / (docs.Count + 0.5));
            Console.WriteLine($"Query term '{term}': in {docs.Count} of {n} documents, idf = {idf:F4}");
            foreach ((int docId, int freq) in docs)
            {
                double tf = freq / (freq + k1 * (1 - b + b * lengths[docId] / avgLength));
                double score = (k1 + 1) * idf * tf;
                scores[docId] = scores.GetValueOrDefault(docId) + score;
                Console.WriteLine($"  doc {docId}: freq {freq}, length {lengths[docId]}, avg length {avgLength:F4}, tf part {tf:F4}, score {score:F4}");
            }
        }

        Console.WriteLine($"Ranking for \"{query}\":");
        foreach ((int docId, double score) in scores.OrderByDescending(s => s.Value))
        {
            Console.WriteLine($"  {score:F4}  {Titles[docId]}");
        }
    }

    // Lowercase with Turkish rules and split on anything that is not a letter or digit.
    private static List<string> Tokenize(string text)
    {
        List<string> terms = [];
        StringBuilder current = new();
        foreach (char c in text.ToLower(Turkish))
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                terms.Add(current.ToString());
                current.Clear();
            }
        }
        if (current.Length > 0)
        {
            terms.Add(current.ToString());
        }
        return terms;
    }
}
