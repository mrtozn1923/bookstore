using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Bookstore.SearchLab;

public static class MappingDemo
{
    // Books where the given person is the translator: a nested query keeps name and role in the same author.
    public static async Task TranslatorsAsync(string[] args)
    {
        string name = args.Length > 0 ? string.Join(' ', args) : "ada demir";
        ElasticsearchClient client = LabClient.Create();

        SearchResponse<Book> response = await client.SearchAsync<Book>(s => s
            .Query(q => q.Nested(n => n
                .Path("authors")
                .Query(nq => nq.Bool(b => b.Must(
                    m => m.Match(mt => mt.Field("authors.name").Query(name).Operator(Operator.And)),
                    m => m.Term(t => t.Field("authors.role").Value("translator"))))))));

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Search failed: {response.ElasticsearchServerError?.Error?.Reason}");
            return;
        }

        Console.WriteLine($"Translated by '{name}': {response.Total}");
        foreach (Book book in response.Documents)
        {
            Console.WriteLine($"  {book.Title} ({book.Language}) - {string.Join(", ", book.Authors.Select(a => $"{a.Name} [{a.Role}]"))}");
        }
    }
}
