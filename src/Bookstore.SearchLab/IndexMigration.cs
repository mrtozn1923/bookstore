using System.Diagnostics;
using System.Globalization;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

namespace Bookstore.SearchLab;

// migrate <new-index> [--rps n]: builds a new books-v* index from the template, copies the current one into it
// and points the "books" alias at it in one atomic request.
public static class IndexMigration
{
    public static async Task RunAsync(string[] args)
    {
        string target = args[0];
        float requestsPerSecond = args.Length > 2 && args[1] == "--rps" ? float.Parse(args[2], CultureInfo.InvariantCulture) : -1;
        string alias = SearchClientFactory.BooksIndex;
        ElasticsearchClient client = LabClient.CreateAdmin();

        // 1. Where does "books" point today? GET /books answers for an alias and for a plain index.
        GetIndexResponse current = await client.Indices.GetAsync(alias);
        string source = current.Indices.Keys.Single();
        bool isAlias = source != alias;
        Console.WriteLine($"\"{alias}\" -> {source} ({(isAlias ? "alias" : "a plain index, not an alias yet")})");

        // 2. The template (books-v*) supplies analysis, mapping, shards and replicas; no refresh during the load.
        CreateIndexResponse created = await client.Indices.CreateAsync(new CreateIndexRequest(target)
        {
            Settings = new IndexSettings { RefreshInterval = Duration.MinusOne }
        });
        if (!created.IsValidResponse)
        {
            Console.WriteLine($"Create {target} failed: {created.ElasticsearchServerError?.Error?.Reason}");
            return;
        }
        Console.WriteLine($"Created {target} from the template, refresh_interval -1");

        // 3. Copy in the background. External versions keep the RowVer versions from SQL Server (post 05).
        Stopwatch watch = Stopwatch.StartNew();
        ReindexResponse started = await client.ReindexAsync(new ReindexRequest
        {
            Source = new Elastic.Clients.Elasticsearch.Core.Reindex.Source { Indices = source },
            Dest = new Elastic.Clients.Elasticsearch.Core.Reindex.Destination { Index = target, VersionType = VersionType.External },
            RequestsPerSecond = requestsPerSecond,
            WaitForCompletion = false
        });
        Console.WriteLine($"Reindex task {started.Task} started, requests_per_second {requestsPerSecond}");

        GetReindexResponse progress;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            progress = await client.GetReindexAsync(started.Task!);
            Console.WriteLine($"  {watch.Elapsed.TotalSeconds,4:F0} s: {progress.Status?.Created:N0} / {progress.Status?.Total:N0} created");
        }
        while (!progress.Completed);

        if (progress.Error is not null || progress.Status?.VersionConflicts > 0)
        {
            Console.WriteLine($"Reindex failed: {progress.Error?.Reason} conflicts {progress.Status?.VersionConflicts}; {alias} still points to {source}");
            return;
        }

        // 4. Back to the template's refresh interval, then compare the counts (top-level books, not nested documents).
        await client.Indices.PutSettingsAsync(new PutIndicesSettingsRequest(target, new IndexSettings { RefreshInterval = "1s" }));
        await client.Indices.RefreshAsync(target);
        long sourceCount = (await client.CountAsync(c => c.Indices(source))).Count;
        long targetCount = (await client.CountAsync(c => c.Indices(target))).Count;
        Console.WriteLine($"{target}: {targetCount:N0} books, {source}: {sourceCount:N0} books");
        if (targetCount != sourceCount)
        {
            Console.WriteLine($"Counts differ; {alias} still points to {source}");
            return;
        }

        // 5. One request, applied together: the alias never points to zero or two indices.
        UpdateAliasesResponse swapped = await client.Indices.UpdateAliasesAsync(new UpdateAliasesRequest
        {
            Actions =
            [
                new AddAction { Index = target, Alias = alias },
                isAlias
                    ? new RemoveAction { Index = source, Alias = alias, MustExist = true }
                    // The first time "books" is a real index: it must go in the same request, or the alias name is taken.
                    // No MustExist here: 9.5.6 rejects it on remove_index, and a missing index fails the whole request anyway.
                    : new RemoveIndexAction { Index = source }
            ]
        });
        Console.WriteLine(swapped.IsValidResponse
            ? $"\"{alias}\" -> {target} ({(isAlias ? $"{source} kept for rollback" : $"index {source} deleted")}), {watch.Elapsed.TotalSeconds:F1} s in total"
            : $"Swap failed: {swapped.ElasticsearchServerError?.Error?.Reason}");
    }
}
