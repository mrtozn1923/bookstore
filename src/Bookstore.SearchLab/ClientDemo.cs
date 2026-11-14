using System.Net;
using System.Text;
using System.Text.Json;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Esql;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Elastic.Transport.Products.Elasticsearch;

namespace Bookstore.SearchLab;

public enum BookFormat { Hardcover, Paperback, EBook }

public sealed record Edition(string Isbn, BookFormat Format, decimal Price, string? Note);

public sealed record CheapBook(string Title, decimal Price);

// Post 10: the same query in two syntaxes, source serialization, error handling and ES|QL from .NET.
public static class ClientDemo
{
    public static async Task SyntaxAsync()
    {
        ElasticsearchClient client = LabClient.Create(debug: true);

        // Fluent: lambdas and descriptors, close to the JSON shape.
        SearchResponse<Book> fluent = await client.SearchAsync<Book>(s => s
            .Size(5)
            .Query(q => q.Bool(b => b
                .Must(m => m.Match(mt => mt.Field(f => f.Title).Query("dune")))
                .Filter(f => f.Term(t => t.Field(x => x.Language).Value("tr"))))));

        // Object initializer: plain objects that can be built step by step, stored and tested.
        SearchRequest<Book> request = new()
        {
            Size = 5,
            Query = new BoolQuery
            {
                Must = [new MatchQuery { Field = "title", Query = "dune" }],
                Filter = [new TermQuery { Field = "language", Value = "tr" }]
            }
        };
        SearchResponse<Book> initializer = await client.SearchAsync<Book>(request);

        string fluentJson = Encoding.UTF8.GetString(fluent.ApiCallDetails.RequestBodyInBytes!);
        string initializerJson = Encoding.UTF8.GetString(initializer.ApiCallDetails.RequestBodyInBytes!);
        Console.WriteLine($"Fluent:      POST {fluent.ApiCallDetails.Uri?.AbsolutePath} {fluentJson} -> {fluent.Total} hit(s)");
        Console.WriteLine($"Initializer: POST {initializer.ApiCallDetails.Uri?.AbsolutePath} {initializerJson} -> {initializer.Total} hit(s)");
        Console.WriteLine($"Same request body: {fluentJson == initializerJson}");
        Console.WriteLine();
        Console.WriteLine(fluent.DebugInformation);
    }

    public static void Serialization()
    {
        Edition edition = new("9781000000030", BookFormat.EBook, 165.75m, null);
        ElasticsearchClient defaults = new(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));
        ElasticsearchClient bookstore = LabClient.Create();
        Console.WriteLine($"Default source serializer:   {defaults.SourceSerializer.SerializeToString(edition)}");
        Console.WriteLine($"Bookstore source serializer: {bookstore.SourceSerializer.SerializeToString(edition)}");
    }

    public static async Task ErrorsAsync()
    {
        ElasticsearchClient client = LabClient.Create();

        // 1) A server error: the index does not exist.
        SearchResponse<Book> missing = await client.SearchAsync<Book>(s => s.Indices("books-missing").Query(q => q.MatchAll()));
        Print("Missing index", missing);

        // 2) A transport error: nothing listens on this port.
        ElasticsearchClient down = SearchClientFactory.Create(new ElasticsearchOptions { Url = "http://localhost:9399", ApiKey = "unused" });
        Print("Unreachable node", await down.SearchAsync<Book>(s => s.Query(q => q.MatchAll())));

        // 3) 429 Too Many Requests from a stub that pretends to be Elasticsearch.
        using HttpListener stub = new();
        stub.Prefixes.Add("http://localhost:9398/");
        stub.Start();
        int calls = 0;
        _ = Task.Run(async () =>
        {
            while (stub.IsListening)
            {
                HttpListenerContext context = await stub.GetContextAsync();
                Interlocked.Increment(ref calls);
                byte[] body = Encoding.UTF8.GetBytes("""{"error":{"type":"es_rejected_execution_exception","reason":"rejected execution of search"},"status":429}""");
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Add("X-Elastic-Product", "Elasticsearch");
                await context.Response.OutputStream.WriteAsync(body);
                context.Response.Close();
            }
        });
        ElasticsearchClient busy = SearchClientFactory.Create(new ElasticsearchOptions { Url = "http://localhost:9398", ApiKey = "unused" });
        Print("429 from the cluster", await busy.SearchAsync<Book>(s => s.Query(q => q.MatchAll())));
        Console.WriteLine($"  requests the stub received: {calls}");
        stub.Stop();
    }

    private static void Print(string title, ElasticsearchResponse response)
    {
        Console.WriteLine($"{title}: IsValidResponse {response.IsValidResponse}, HTTP {response.ApiCallDetails.HttpStatusCode?.ToString() ?? "none"}");
        Console.WriteLine($"  server error: {response.ElasticsearchServerError?.Error?.Type} {response.ElasticsearchServerError?.Error?.Reason}");
        Console.WriteLine($"  exception:    {response.ApiCallDetails.OriginalException?.GetType().Name} {response.ApiCallDetails.OriginalException?.Message}");
    }

    public static async Task EsqlAsync()
    {
        // Prints the ES|QL text the LINQ query below is translated to.
        ElasticsearchClient client = LabClient.Create(debug: true, call =>
        {
            if (call.Uri?.AbsolutePath == "/_query" && call.RequestBodyInBytes is byte[] body)
            {
                Console.WriteLine($"  POST /_query {Encoding.UTF8.GetString(body)}");
            }
        });

        // 1) A query string; the JSON body is streamed back as columns and rows. (format=txt came back as an invalid response.)
        using ElasticsearchStreamResponse raw = await client.Esql.QueryAsync(q => q
            .Query("FROM books | WHERE language == \"tr\" | STATS books = COUNT(*), avg_price = AVG(price) BY categories | SORT books DESC | LIMIT 3"));
        using JsonDocument result = await JsonDocument.ParseAsync(raw.Body);
        Console.WriteLine($"Raw ES|QL: IsValidResponse {raw.IsValidResponse}, HTTP {raw.ApiCallDetails.HttpStatusCode}");
        Console.WriteLine("  " + string.Join(" | ", result.RootElement.GetProperty("columns").EnumerateArray().Select(c => c.GetProperty("name").GetString())));
        foreach (JsonElement row in result.RootElement.GetProperty("values").EnumerateArray())
        {
            Console.WriteLine("  " + string.Join(" | ", row.EnumerateArray().Select(v => v.ToString())));
        }

        // 2) LINQ, translated to ES|QL by Elastic.Esql and read row by row; the projection keeps only what we need.
        await foreach (CheapBook book in client.Esql.QueryAsync<Book, CheapBook>(q => q
            .Where(b => b.Language == "tr" && b.Price < 170)
            .OrderBy(b => b.Price)
            .Take(3)
            .Select(b => new CheapBook(b.Title, b.Price))))
        {
            Console.WriteLine($"  {book.Title}: {book.Price}");
        }
    }
}
