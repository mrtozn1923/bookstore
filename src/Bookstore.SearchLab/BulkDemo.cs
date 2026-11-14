using System.Diagnostics;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;

namespace Bookstore.SearchLab;

public static class BulkDemo
{
    private const string LoadIndex = "books-load";

    // One request per document versus the Bulk API, on an empty books-load index.
    public static async Task CompareAsync()
    {
        ElasticsearchClient client = LabClient.Create();
        List<Book> books = SyntheticBooks.Generate(20_000).ToList();

        Stopwatch watch = Stopwatch.StartNew();
        foreach (Book book in books.Take(2_000))
        {
            IndexResponse response = await client.IndexAsync(book, i => i.Index(LoadIndex));
            if (!response.IsValidResponse)
            {
                Console.WriteLine($"Index failed: {response.ElasticsearchServerError?.Error?.Reason}");
                return;
            }
        }
        Console.WriteLine($"IndexAsync x 2,000: {watch.ElapsedMilliseconds:N0} ms");

        watch.Restart();
        foreach (Book[] chunk in books.Chunk(1_000))
        {
            BulkResponse response = await client.BulkAsync(b => b.Index(LoadIndex).IndexMany(chunk));
            if (response.Errors)
            {
                Console.WriteLine($"Bulk had {response.ItemsWithErrors.Count()} failed items");
            }
        }
        Console.WriteLine($"BulkAsync 20 x 1,000: {watch.ElapsedMilliseconds:N0} ms");
    }

    // BulkAll: the client splits the stream into pages, sends them in parallel and retries 429s.
    public static void LoadAll(string[] args)
    {
        int count = args.Length > 0 ? int.Parse(args[0]) : 100_000;
        ElasticsearchClient client = LabClient.Create();
        Stopwatch watch = Stopwatch.StartNew();

        BulkAllObservable<Book> bulkAll = client.BulkAll(SyntheticBooks.Generate(count), b => b
            .Index(LoadIndex)
            .Size(2_000)
            .MaxDegreeOfParallelism(2)
            .BackOffRetries(3)
            .BackOffTime(TimeSpan.FromSeconds(2))
            .DroppedDocumentCallback((item, book) => Console.WriteLine($"Dropped {book.Isbn}: {item.Error?.Reason}")));

        int pages = 0;
        bulkAll.Wait(TimeSpan.FromMinutes(10), response => pages++);
        Console.WriteLine($"BulkAll: {count:N0} books in {pages} pages, {watch.ElapsedMilliseconds:N0} ms");
    }

    // A bulk request is not atomic: every item succeeds or fails on its own.
    public static async Task ErrorsAsync()
    {
        ElasticsearchClient client = LabClient.Create();
        IReadOnlyList<Book> books = await BookCatalog.LoadAsync(Path.Combine("data", "books.json"));

        BulkRequest request = new(LoadIndex)
        {
            Operations =
            [
                new BulkIndexOperation<Book>(books[0]) { Id = books[0].Isbn },
                // Not in the mapping, and the mapping is "dynamic": "strict"
                new BulkIndexOperation<object>(new { isbn = "9789999999991", title = "Draft", pageCount = 412 }) { Id = "9789999999991" },
                new BulkCreateOperation<Book>(books[1]) { Id = books[1].Isbn },
                new BulkCreateOperation<Book>(books[1]) { Id = books[1].Isbn }
            ]
        };

        BulkResponse response = await client.BulkAsync(request);
        Console.WriteLine($"HTTP {response.ApiCallDetails.HttpStatusCode}, IsValidResponse {response.IsValidResponse}, errors {response.Errors}");
        foreach (ResponseItem item in response.Items)
        {
            Console.WriteLine(item.Error is null
                ? $"  {item.Operation} {item.Id}: {item.Status} {item.Result}"
                : $"  {item.Operation} {item.Id}: {item.Status} {item.Error.Type}: {item.Error.Reason}");
        }
    }

    // Near-real-time: an indexed document is searchable only after the next refresh.
    public static async Task RefreshAsync()
    {
        ElasticsearchClient client = LabClient.Create();
        Book book = SyntheticBooks.Generate(1).Single() with { Isbn = "9789999999984", Title = "Refresh Test" };

        await client.IndexAsync(book, i => i.Index(LoadIndex));
        Console.WriteLine($"After index:              {await CountAsync(client, book.Isbn)} hit(s)");

        await Task.Delay(TimeSpan.FromSeconds(2));
        Console.WriteLine($"Two seconds later:        {await CountAsync(client, book.Isbn)} hit(s)");

        Book second = book with { Isbn = "9789999999977" };
        Stopwatch watch = Stopwatch.StartNew();
        await client.IndexAsync(second, i => i.Index(LoadIndex).Refresh(Refresh.WaitFor));
        Console.WriteLine($"After refresh=wait_for:   {await CountAsync(client, second.Isbn)} hit(s), index call took {watch.ElapsedMilliseconds} ms");
    }

    private static async Task<long> CountAsync(ElasticsearchClient client, string isbn)
    {
        CountResponse count = await client.CountAsync<Book>(c => c
            .Indices(LoadIndex)
            .Query(q => q.Term(t => t.Field("isbn").Value(isbn))));
        return count.Count;
    }

    // Optimistic concurrency: write only if nobody changed the document since we read it.
    public static async Task ConcurrencyAsync()
    {
        ElasticsearchClient client = LabClient.Create();
        IReadOnlyList<Book> books = await BookCatalog.LoadAsync(Path.Combine("data", "books.json"));
        Book dune = books.First(b => b.Title == "Dune" && b.Language == "en");

        GetResponse<Book> read = await client.GetAsync<Book>(dune.Isbn, g => g.Index(LoadIndex));
        if (!read.Found)
        {
            await client.IndexAsync(dune, i => i.Index(LoadIndex).Refresh(Refresh.WaitFor));
            read = await client.GetAsync<Book>(dune.Isbn, g => g.Index(LoadIndex));
        }
        Console.WriteLine($"Read: price {read.Source!.Price}, _seq_no {read.SeqNo}, _primary_term {read.PrimaryTerm}");

        // Two writers both read the same _seq_no and both try to write.
        IndexResponse first = await client.IndexAsync(read.Source with { Price = 499.00m }, i => i
            .Index(LoadIndex).IfSeqNo(read.SeqNo!.Value).IfPrimaryTerm(read.PrimaryTerm!.Value));
        Console.WriteLine($"Writer A: {first.ApiCallDetails.HttpStatusCode} {first.Result}, _seq_no {first.SeqNo}");

        IndexResponse second = await client.IndexAsync(read.Source with { Price = 450.00m }, i => i
            .Index(LoadIndex).IfSeqNo(read.SeqNo!.Value).IfPrimaryTerm(read.PrimaryTerm!.Value));
        Console.WriteLine($"Writer B: {second.ApiCallDetails.HttpStatusCode} {second.ElasticsearchServerError?.Error?.Type}");
        Console.WriteLine($"          {second.ElasticsearchServerError?.Error?.Reason}");

        // External versioning: the version comes from the source system (SQL Server rowversion in the sync).
        IndexResponse v10 = await client.IndexAsync(dune with { Price = 510.00m }, i => i
            .Index(LoadIndex).Id("external-demo").Version(10).VersionType(VersionType.External));
        IndexResponse v9 = await client.IndexAsync(dune with { Price = 505.00m }, i => i
            .Index(LoadIndex).Id("external-demo").Version(9).VersionType(VersionType.External));
        Console.WriteLine($"External version 10: {v10.ApiCallDetails.HttpStatusCode} {v10.Result}");
        Console.WriteLine($"External version 9:  {v9.ApiCallDetails.HttpStatusCode} {v9.ElasticsearchServerError?.Error?.Type}");
    }
}
