using System.Diagnostics;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

namespace Bookstore.SearchLab;

// Walks the whole books index with a point in time (PIT) and search_after.
public static class ExportDemo
{
    public static async Task RunAsync(string[] args)
    {
        int pageSize = args.Length > 0 ? int.Parse(args[0]) : 1_000;
        ElasticsearchClient client = LabClient.Create();
        Stopwatch watch = Stopwatch.StartNew();

        OpenPointInTimeResponse pit = await client.OpenPointInTimeAsync(SearchClientFactory.BooksIndex, p => p.KeepAlive("1m"));
        if (!pit.IsValidResponse)
        {
            Console.WriteLine($"Open PIT failed: {pit.ElasticsearchServerError?.Error?.Reason}");
            return;
        }

        string pitId = pit.Id;
        ICollection<FieldValue>? after = null;
        int pages = 0;
        long books = 0;
        string? lastIsbn = null;
        try
        {
            while (true)
            {
                // No index name with a PIT: the PIT already pins the index and its state.
                SearchResponse<Book> page = await client.SearchAsync<Book>(s =>
                {
                    s.Size(pageSize)
                     .Pit(p => p.Id(pitId).KeepAlive("1m"))
                     .Sort(so => so.Field("isbn", f => f.Order(SortOrder.Asc)))
                     .TrackTotalHits(new TrackHits(false));
                    if (after is not null)
                    {
                        s.SearchAfter(after);
                    }
                });

                if (!page.IsValidResponse)
                {
                    Console.WriteLine($"Page {pages + 1} failed: {page.ElasticsearchServerError?.Error?.Reason}");
                    return;
                }
                if (page.Hits.Count == 0)
                {
                    break;
                }

                pages++;
                books += page.Hits.Count;
                Hit<Book> last = page.Hits.Last();
                after = last.Sort!.ToList();
                lastIsbn = last.Source!.Isbn;
                pitId = page.PitId ?? pitId;
            }
        }
        finally
        {
            await client.ClosePointInTimeAsync(c => c.Id(pitId));
        }

        Console.WriteLine($"Exported {books:N0} books in {pages} pages of {pageSize:N0}, last ISBN {lastIsbn}, {watch.ElapsedMilliseconds:N0} ms");
    }
}
